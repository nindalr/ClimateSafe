using ClimateSafe.Api.Models;
using Npgsql;

namespace ClimateSafe.Api.Data;

public sealed class QuestionCatalogRepository(IConfiguration configuration)
{
    public async Task<IReadOnlyList<AssessmentQuestion>?> GetQuestionsAsync(int hazardId, CancellationToken ct)
    {
        string connectionString = configuration.GetConnectionString("ClimateSafe")
            ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT q.question_id, q.dimension_id, d.name, q.question_text, q.description,
                   o.option_id, o.option_text, o.display_order
            FROM hazards h
            LEFT JOIN questions q ON q.hazard_id = h.hazard_id
            LEFT JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
            LEFT JOIN question_options o ON o.question_id = q.question_id
            WHERE h.hazard_id = @hazard
            ORDER BY q.dimension_id, q.question_id, o.display_order, o.option_id
            """, connection);
        command.Parameters.AddWithValue("hazard", hazardId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        var questions = new List<AssessmentQuestion>();
        bool hazardFound = false;
        int? currentId = null;
        List<QuestionOption>? options = null;
        while (await reader.ReadAsync(ct))
        {
            hazardFound = true;
            if (reader.IsDBNull(0)) continue;
            int questionId = reader.GetInt32(0);
            if (questionId != currentId)
            {
                options = new List<QuestionOption>();
                questions.Add(new AssessmentQuestion(questionId, reader.GetInt32(1), reader.GetString(2),
                    reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), options));
                currentId = questionId;
            }
            if (!reader.IsDBNull(5))
                options!.Add(new QuestionOption(reader.GetInt32(5), reader.GetString(6), reader.GetInt32(7)));
        }
        return hazardFound ? questions : null;
    }
}
