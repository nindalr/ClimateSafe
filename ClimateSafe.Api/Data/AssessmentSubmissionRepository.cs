using ClimateSafe.Api.Models;
using ClimateSafe.Api.Services;
using Npgsql;

namespace ClimateSafe.Api.Data;

public sealed class AssessmentSubmissionRepository(IConfiguration configuration)
{
    public async Task<AssessmentSubmissionResult> SubmitAsync(int userId, AssessmentSubmissionRequest request, CancellationToken ct)
    {
        if (request.SubmissionKey == Guid.Empty || request.HazardId < 1 || request.HazardId > 3 ||
            request.OptionIds == null || request.OptionIds.Count == 0 ||
            request.OptionIds.Any(id => id <= 0) || request.OptionIds.Distinct().Count() != request.OptionIds.Count)
            throw new ArgumentException("Kunci, hazard, dan pilihan jawaban harus valid dan tidak berulang.");

        string connectionString = configuration.GetConnectionString("ClimateSafe")
            ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var active = new NpgsqlCommand("SELECT 1 FROM users WHERE user_id = @user AND status = 'Active' FOR SHARE", connection, transaction))
        {
            active.Parameters.AddWithValue("user", userId);
            if (await active.ExecuteScalarAsync(ct) == null) throw new UnauthorizedAccessException();
        }
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))", connection, transaction))
        {
            gate.Parameters.AddWithValue("key", request.SubmissionKey.ToString());
            await gate.ExecuteNonQueryAsync(ct);
        }

        AssessmentSubmissionResult? existing = null;
        await using (var command = new NpgsqlCommand("""
            SELECT assessment_id, user_id, hazard_id, overall_score, level
            FROM assessments WHERE submission_key = @key
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("key", request.SubmissionKey);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                if (reader.GetInt32(1) != userId || reader.GetInt32(2) != request.HazardId)
                    throw new SubmissionConflictException();
                existing = new AssessmentSubmissionResult(reader.GetInt32(0), reader.GetDouble(3), reader.GetString(4), false);
            }
        }
        if (existing != null)
        {
            var storedOptions = new List<int>();
            await using var command = new NpgsqlCommand(
                "SELECT option_id FROM assessment_submission_options WHERE assessment_id = @id ORDER BY option_id", connection, transaction);
            command.Parameters.AddWithValue("id", existing.AssessmentId);
            await using (var reader = await command.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct)) storedOptions.Add(reader.GetInt32(0));
            if (!storedOptions.SequenceEqual(request.OptionIds.OrderBy(id => id))) throw new SubmissionConflictException();
            await transaction.CommitAsync(ct);
            return existing;
        }

        var questionIds = new List<int>();
        await using (var command = new NpgsqlCommand("SELECT question_id FROM questions WHERE hazard_id = @hazard ORDER BY question_id FOR SHARE", connection, transaction))
        {
            command.Parameters.AddWithValue("hazard", request.HazardId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) questionIds.Add(reader.GetInt32(0));
        }
        if (questionIds.Count == 0 || questionIds.Count != request.OptionIds.Count)
            throw new ArgumentException("Setiap pertanyaan harus dijawab tepat satu kali.");

        var answers = new List<ScoredAnswer>();
        await using (var command = new NpgsqlCommand("""
            SELECT q.question_id, q.dimension_id, d.name, q.question_text, o.option_id,
                   o.option_text, o.score, d.passing_threshold
            FROM question_options o JOIN questions q ON q.question_id = o.question_id
            JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
            WHERE q.hazard_id = @hazard AND o.option_id = ANY(@options)
            ORDER BY q.dimension_id FOR SHARE OF q, d, o
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("hazard", request.HazardId);
            command.Parameters.AddWithValue("options", request.OptionIds.ToArray());
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                answers.Add(new ScoredAnswer(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                    reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetInt32(6), reader.GetDouble(7)));
        }
        if (answers.Count != questionIds.Count || answers.Select(a => a.QuestionId).Distinct().Count() != questionIds.Count ||
            !answers.Select(a => a.QuestionId).OrderBy(id => id).SequenceEqual(questionIds))
            throw new ArgumentException("Pilihan harus sesuai hazard dan menjawab setiap pertanyaan tepat satu kali.");

        var (score, level) = AssessmentScoringService.Evaluate(answers);
        int assessmentId;
        await using (var command = new NpgsqlCommand("""
            INSERT INTO assessments (submission_key, user_id, hazard_id, overall_score, level)
            VALUES (@key, @user, @hazard, @score, @level) RETURNING assessment_id
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("key", request.SubmissionKey);
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("hazard", request.HazardId);
            command.Parameters.AddWithValue("score", score);
            command.Parameters.AddWithValue("level", level);
            assessmentId = (int)(await command.ExecuteScalarAsync(ct))!;
        }

        foreach (var answer in answers)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO assessment_answers (assessment_id, question_id, value, score, question_text, dimension_name)
                VALUES (@assessment, @question, @value, @score, @text, @dimension);
                INSERT INTO assessment_submission_options (assessment_id, question_id, option_id)
                VALUES (@assessment, @question, @option)
                """, connection, transaction);
            command.Parameters.AddWithValue("assessment", assessmentId);
            command.Parameters.AddWithValue("question", answer.QuestionId);
            command.Parameters.AddWithValue("value", answer.Value);
            command.Parameters.AddWithValue("score", answer.Score);
            command.Parameters.AddWithValue("text", answer.QuestionText);
            command.Parameters.AddWithValue("dimension", answer.DimensionName);
            command.Parameters.AddWithValue("option", answer.OptionId);
            await command.ExecuteNonQueryAsync(ct);
        }

        foreach (var answer in answers.Where(AssessmentScoringService.IsGap).OrderBy(a => a.DimensionId))
        {
            string description;
            await using (var template = new NpgsqlCommand("SELECT description FROM recommendation_templates WHERE hazard_id = @hazard AND dimension_id = @dimension FOR SHARE", connection, transaction))
            {
                template.Parameters.AddWithValue("hazard", request.HazardId);
                template.Parameters.AddWithValue("dimension", answer.DimensionId);
                description = (await template.ExecuteScalarAsync(ct) as string)
                    ?? throw new InvalidOperationException("Katalog rekomendasi belum lengkap.");
            }
            int recommendationId;
            await using (var command = new NpgsqlCommand("""
                INSERT INTO recommendations (recommendation_id, hazard_id, dimension_id, description, priority)
                VALUES (@id, @hazard, @dimension, @description, @priority)
                ON CONFLICT (hazard_id, dimension_id) DO UPDATE
                SET description = EXCLUDED.description, priority = EXCLUDED.priority
                RETURNING recommendation_id
                """, connection, transaction))
            {
                command.Parameters.AddWithValue("id", request.HazardId * 100 + answer.DimensionId);
                command.Parameters.AddWithValue("hazard", request.HazardId);
                command.Parameters.AddWithValue("dimension", answer.DimensionId);
                command.Parameters.AddWithValue("description", description);
                command.Parameters.AddWithValue("priority", AssessmentScoringService.Priority(answer));
                recommendationId = (int)(await command.ExecuteScalarAsync(ct))!;
            }
            await using var checklist = new NpgsqlCommand("""
                INSERT INTO checklist_items (assessment_id, recommendation_id, description)
                VALUES (@assessment, @recommendation, @description)
                """, connection, transaction);
            checklist.Parameters.AddWithValue("assessment", assessmentId);
            checklist.Parameters.AddWithValue("recommendation", recommendationId);
            checklist.Parameters.AddWithValue("description", description);
            await checklist.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return new AssessmentSubmissionResult(assessmentId, score, level, true);
    }
}

public sealed class SubmissionConflictException : Exception;
