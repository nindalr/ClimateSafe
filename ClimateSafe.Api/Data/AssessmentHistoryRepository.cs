using System.Data;
using ClimateSafe.Api.Models;
using Npgsql;

namespace ClimateSafe.Api.Data;

public sealed class AssessmentHistoryRepository(IConfiguration configuration)
{
    private const double MaximumAnswerScore = ClimateSafe.Api.Services.AssessmentScoringService.MaximumAnswerScore;

    private NpgsqlConnection CreateConnection() => new(configuration.GetConnectionString("ClimateSafe")
        ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi."));

    private const string SummaryQuery = """
        SELECT a.assessment_id, a.hazard_id, h.name, a.created_at, a.overall_score, a.level,
               count(c.checklist_item_id)::int, count(c.checklist_item_id) FILTER (WHERE c.is_completed)::int
        FROM assessments a JOIN hazards h ON h.hazard_id = a.hazard_id
        LEFT JOIN checklist_items c ON c.assessment_id = a.assessment_id
        WHERE a.user_id = @user
        """;

    private const string SummaryGroup = """
        GROUP BY a.assessment_id, h.name
        """;

    private static AssessmentSummary ReadSummary(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDateTime(3),
        reader.GetDouble(4), reader.GetString(5), reader.GetInt32(6), reader.GetInt32(7));

    private static ChecklistItem ReadItem(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3),
        reader.GetBoolean(4), reader.IsDBNull(5) ? null : reader.GetDateTime(5));

    public async Task<IReadOnlyList<AssessmentSession>> GetSessionsAsync(int userId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT a.assessment_id, h.name, a.created_at, a.overall_score, a.level,
                   c.checklist_item_id, c.recommendation_id, c.description, c.is_completed, c.completed_at
            FROM assessments a JOIN hazards h ON h.hazard_id = a.hazard_id
            LEFT JOIN checklist_items c ON c.assessment_id = a.assessment_id
            WHERE a.user_id = @user
            ORDER BY a.created_at DESC, a.assessment_id DESC, c.checklist_item_id
            """, connection);
        command.Parameters.AddWithValue("user", userId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var sessions = new List<AssessmentSession>();
        List<ChecklistItem>? items = null;
        int currentId = 0;
        while (await reader.ReadAsync(ct))
        {
            int id = reader.GetInt32(0);
            if (id != currentId)
            {
                items = new List<ChecklistItem>();
                sessions.Add(new AssessmentSession(id, reader.GetString(1), reader.GetDateTime(2),
                    reader.GetDouble(3), reader.GetString(4), items));
                currentId = id;
            }
            if (!reader.IsDBNull(5))
                items!.Add(new ChecklistItem(reader.GetInt32(5), id, reader.GetInt32(6), reader.GetString(7),
                    reader.GetBoolean(8), reader.IsDBNull(9) ? null : reader.GetDateTime(9)));
        }
        return sessions;
    }

    public async Task<IReadOnlyList<AssessmentSummary>> GetAllAsync(int userId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(SummaryQuery + "\n" + SummaryGroup +
            " ORDER BY a.created_at DESC, a.assessment_id DESC", connection);
        command.Parameters.AddWithValue("user", userId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<AssessmentSummary>();
        while (await reader.ReadAsync(ct)) result.Add(ReadSummary(reader));
        return result;
    }

    public async Task<AssessmentDetail?> GetDetailAsync(int userId, int assessmentId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        AssessmentSummary? summary;
        await using (var command = new NpgsqlCommand(SummaryQuery + " AND a.assessment_id = @assessment\n" + SummaryGroup, connection, transaction))
        {
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("assessment", assessmentId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            summary = await reader.ReadAsync(ct) ? ReadSummary(reader) : null;
        }
        if (summary == null) return null;

        var answers = new List<AssessmentAnswer>();
        var dimensions = new List<DimensionScore>();
        await using (var command = new NpgsqlCommand("""
            SELECT q.dimension_id, COALESCE(aa.dimension_name, d.name),
                   COALESCE(aa.question_text, q.question_text), aa.value, aa.score, d.passing_threshold
            FROM assessments a
            JOIN assessment_answers aa ON aa.assessment_id = a.assessment_id
            JOIN questions q ON q.question_id = aa.question_id
            JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
            WHERE a.user_id = @user AND a.assessment_id = @assessment
            ORDER BY q.dimension_id, q.question_id
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("assessment", assessmentId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var scores = new Dictionary<int, (string Name, double Sum, int Count, double Threshold)>();
            while (await reader.ReadAsync(ct))
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                int score = reader.GetInt32(4);
                answers.Add(new AssessmentAnswer(name, reader.GetString(2), reader.GetString(3), score));
                if (!scores.TryGetValue(id, out var existing))
                    existing = (name, 0, 0, reader.GetDouble(5));
                scores[id] = (existing.Name, existing.Sum + score, existing.Count + 1, existing.Threshold);
            }
            foreach (var (id, value) in scores)
                dimensions.Add(new DimensionScore(id, value.Name,
                    value.Sum / value.Count / MaximumAnswerScore * 100, value.Threshold));
        }

        var checklist = new List<ChecklistItem>();
        await using (var command = new NpgsqlCommand("""
            SELECT c.checklist_item_id, c.assessment_id, c.recommendation_id,
                   c.description, c.is_completed, c.completed_at
            FROM checklist_items c JOIN assessments a ON a.assessment_id = c.assessment_id
            WHERE a.user_id = @user AND a.assessment_id = @assessment
            ORDER BY c.checklist_item_id
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("assessment", assessmentId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) checklist.Add(ReadItem(reader));
        }
        var recommendations = new List<AssessmentRecommendation>();
        await using (var command = new NpgsqlCommand("""
            SELECT dimension_id, description FROM recommendation_templates WHERE hazard_id = @hazard
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("hazard", summary.HazardId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var descriptions = new Dictionary<int, string>();
            while (await reader.ReadAsync(ct)) descriptions[reader.GetInt32(0)] = reader.GetString(1);
            foreach (var dimension in dimensions.Where(d => d.Score < d.PassingThreshold).OrderBy(d => d.Score))
            {
                if (!descriptions.TryGetValue(dimension.DimensionId, out string? description))
                    throw new InvalidOperationException("Katalog rekomendasi belum lengkap.");
                recommendations.Add(new AssessmentRecommendation(summary.HazardId * 100 + dimension.DimensionId,
                    dimension.DimensionId, description, ClimateSafe.Api.Services.AssessmentScoringService.Priority(dimension.Score)));
            }
        }
        await transaction.CommitAsync(ct);
        return new AssessmentDetail(summary, answers, dimensions, checklist, recommendations);
    }

    public async Task<ChecklistItem?> SetCompletedAsync(int userId, int itemId, bool completed, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE checklist_items c
            SET is_completed = @completed,
                completed_at = CASE WHEN @completed THEN COALESCE(c.completed_at, CURRENT_TIMESTAMP) ELSE NULL END
            FROM assessments a
            WHERE c.assessment_id = a.assessment_id AND a.user_id = @user AND c.checklist_item_id = @id
            RETURNING c.checklist_item_id, c.assessment_id, c.recommendation_id,
                      c.description, c.is_completed, c.completed_at
            """, connection);
        command.Parameters.AddWithValue("completed", completed);
        command.Parameters.AddWithValue("user", userId);
        command.Parameters.AddWithValue("id", itemId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadItem(reader) : null;
    }
}
