using System;
using System.Collections.Generic;
using System.Linq;
using ClimateSafe.Auth;
using Npgsql;

namespace ClimateSafe
{
    internal sealed class ChecklistRepository
    {
        private readonly Func<NpgsqlConnection> createConnection;

        public ChecklistRepository(Func<NpgsqlConnection>? createConnection = null)
        {
            this.createConnection = createConnection ?? Database.Create;
        }

        public int SaveAssessment(int userId, Guid submissionKey, Hazard hazard, Assessment assessment,
            IReadOnlyList<Recommendation> recommendations, IReadOnlyList<ChecklistItem> items)
        {
            ArgumentNullException.ThrowIfNull(hazard);
            ArgumentNullException.ThrowIfNull(assessment);
            if (userId <= 0 || submissionKey == Guid.Empty)
                throw new ArgumentException("Pengguna dan kunci sesi asesmen harus valid.");
            if (hazard.HazardId < 1 || hazard.HazardId > 3)
                throw new ArgumentException("Jenis bencana belum didukung.");
            var questions = hazard.GetQuestions().ToDictionary(question => question.QuestionId);
            var answers = assessment.GetAnswers();
            if (questions.Count != 5 || answers.Count != questions.Count ||
                answers.Any(answer => !questions.ContainsKey(answer.Question.QuestionId)) ||
                questions.Keys.Any(id => id < 1 || id > 5))
                throw new ArgumentException("Sesi harus memiliki jawaban untuk kelima dimensi.");
            // Gunakan hasil perhitungan model agar skor yang disimpan konsisten dengan jawaban.
            double score = assessment.CalculateOverallScore();

            using var connection = createConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var active = new NpgsqlCommand(
                "SELECT 1 FROM users WHERE user_id = @user AND status = 'Active' FOR SHARE", connection, transaction);
            active.Parameters.AddWithValue("user", userId);
            if (active.ExecuteScalar() == null)
                throw new InvalidOperationException("Akun pengguna tidak ditemukan atau tidak aktif.");

            using var insert = new NpgsqlCommand("""
                INSERT INTO assessments(submission_key, user_id, hazard_id, overall_score, level, created_at)
                VALUES (@key, @user, @hazard, @score, @level, @date)
                ON CONFLICT (submission_key) DO NOTHING RETURNING assessment_id
                """, connection, transaction);
            insert.Parameters.AddWithValue("key", submissionKey);
            insert.Parameters.AddWithValue("user", userId);
            insert.Parameters.AddWithValue("hazard", hazard.HazardId);
            insert.Parameters.AddWithValue("score", score);
            insert.Parameters.AddWithValue("level", assessment.Level);
            insert.Parameters.AddWithValue("date", assessment.AssessmentDate.ToUniversalTime());
            object? inserted = insert.ExecuteScalar();
            if (inserted == null)
            {
                using var existing = new NpgsqlCommand("""
                    SELECT assessment_id FROM assessments
                    WHERE submission_key = @key AND user_id = @user AND hazard_id = @hazard
                    """, connection, transaction);
                existing.Parameters.AddWithValue("key", submissionKey);
                existing.Parameters.AddWithValue("user", userId);
                existing.Parameters.AddWithValue("hazard", hazard.HazardId);
                object? existingId = existing.ExecuteScalar();
                if (existingId == null) throw new InvalidOperationException("Kunci sesi sudah digunakan.");
                int id = Convert.ToInt32(existingId);
                using var stored = new NpgsqlCommand("""
                    SELECT q.dimension_id, aa.value, aa.score
                    FROM assessment_answers aa JOIN questions q ON q.question_id = aa.question_id
                    WHERE aa.assessment_id = @assessment ORDER BY q.dimension_id
                    """, connection, transaction);
                stored.Parameters.AddWithValue("assessment", id);
                using (var reader = stored.ExecuteReader())
                {
                    var expected = answers.OrderBy(answer => answer.Question.QuestionId).ToList();
                    int index = 0;
                    while (reader.Read())
                    {
                        if (index >= expected.Count || reader.GetInt32(0) != expected[index].Question.QuestionId ||
                            reader.GetString(1) != expected[index].Value || reader.GetInt32(2) != expected[index].Score)
                            throw new InvalidOperationException("Kunci sesi sudah digunakan untuk jawaban berbeda.");
                        index++;
                    }
                    if (index != expected.Count) throw new InvalidOperationException("Data sesi tidak lengkap.");
                }
                transaction.Commit();
                return id;
            }
            int assessmentId = Convert.ToInt32(inserted);

            foreach (Answer answer in answers.OrderBy(answer => answer.Question.QuestionId))
            {
                int questionId = hazard.HazardId * 100 + answer.Question.QuestionId;
                using var command = new NpgsqlCommand("""
                    INSERT INTO questions(question_id, hazard_id, dimension_id, question_text)
                    VALUES (@question, @hazard, @dimension, @text)
                    ON CONFLICT (question_id) DO UPDATE SET question_text = EXCLUDED.question_text;
                    INSERT INTO assessment_answers(assessment_id, question_id, value, score)
                    VALUES (@assessment, @question, @value, @score)
                    """, connection, transaction);
                command.Parameters.AddWithValue("question", questionId);
                command.Parameters.AddWithValue("hazard", hazard.HazardId);
                command.Parameters.AddWithValue("dimension", answer.Question.QuestionId);
                command.Parameters.AddWithValue("text", questions[answer.Question.QuestionId].QuestionText);
                command.Parameters.AddWithValue("assessment", assessmentId);
                command.Parameters.AddWithValue("value", answer.Value);
                command.Parameters.AddWithValue("score", answer.Score);
                command.ExecuteNonQuery();
            }

            foreach (Recommendation recommendation in recommendations.OrderBy(item => item.RecommendationId))
            {
                int dimensionId = recommendation.RecommendationId - hazard.HazardId * 100;
                if (dimensionId < 1 || dimensionId > 5)
                    throw new ArgumentException("Rekomendasi tidak sesuai dengan jenis bencana.");
                using var command = new NpgsqlCommand("""
                    INSERT INTO recommendations(recommendation_id, hazard_id, dimension_id, description, priority)
                    VALUES (@id, @hazard, @dimension, @description, @priority)
                    ON CONFLICT (recommendation_id) DO UPDATE
                    SET description = EXCLUDED.description, priority = EXCLUDED.priority
                    """, connection, transaction);
                command.Parameters.AddWithValue("id", recommendation.RecommendationId);
                command.Parameters.AddWithValue("hazard", hazard.HazardId);
                command.Parameters.AddWithValue("dimension", dimensionId);
                command.Parameters.AddWithValue("description", recommendation.Description);
                command.Parameters.AddWithValue("priority", recommendation.Priority);
                command.ExecuteNonQuery();
            }
            foreach (ChecklistItem item in items)
            {
                using var command = new NpgsqlCommand("""
                    INSERT INTO checklist_items(assessment_id, recommendation_id, description)
                    VALUES (@assessment, @recommendation, @description)
                    """, connection, transaction);
                command.Parameters.AddWithValue("assessment", assessmentId);
                command.Parameters.AddWithValue("recommendation", item.RecommendationId);
                command.Parameters.AddWithValue("description", item.Description);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
            return assessmentId;
        }

        public IReadOnlyList<ChecklistSession> GetSessions(int userId)
        {
            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT a.assessment_id, h.name, a.created_at, a.overall_score, a.level,
                       c.checklist_item_id, c.recommendation_id, c.description, c.is_completed, c.completed_at
                FROM assessments a JOIN hazards h ON h.hazard_id = a.hazard_id
                LEFT JOIN checklist_items c ON c.assessment_id = a.assessment_id
                WHERE a.user_id = @user
                ORDER BY a.created_at DESC, a.assessment_id DESC, c.checklist_item_id
                """, connection);
            command.Parameters.AddWithValue("user", userId);
            using var reader = command.ExecuteReader();
            var sessions = new List<ChecklistSession>();
            var items = new List<ChecklistItem>();
            int currentId = -1;
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                if (id != currentId)
                {
                    items = new List<ChecklistItem>();
                    sessions.Add(new ChecklistSession(id, reader.GetString(1), reader.GetDateTime(2),
                        reader.GetDouble(3), reader.GetString(4), items.AsReadOnly()));
                    currentId = id;
                }
                if (!reader.IsDBNull(5)) items.Add(ReadItem(reader, id, 5));
            }
            return sessions.AsReadOnly();
        }

        public ChecklistItem SetCompleted(int userId, int itemId, bool completed)
        {
            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                UPDATE checklist_items c
                SET is_completed = @completed,
                    completed_at = CASE WHEN @completed THEN COALESCE(c.completed_at, CURRENT_TIMESTAMP) ELSE NULL END
                FROM assessments a
                WHERE c.assessment_id = a.assessment_id AND a.user_id = @user AND c.checklist_item_id = @id
                RETURNING c.checklist_item_id, c.recommendation_id, c.description, c.is_completed,
                          c.completed_at, c.assessment_id
                """, connection);
            command.Parameters.AddWithValue("completed", completed);
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("id", itemId);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException("Checklist tidak ditemukan untuk pengguna ini.");
            return ReadItem(reader, reader.GetInt32(5), 0);
        }

        private static ChecklistItem ReadItem(NpgsqlDataReader reader, int assessmentId, int offset) =>
            new ChecklistItem(reader.GetInt32(offset), reader.GetString(offset + 2), assessmentId,
                reader.GetInt32(offset + 1), reader.GetBoolean(offset + 3),
                reader.IsDBNull(offset + 4) ? null : reader.GetDateTime(offset + 4));
    }
}
