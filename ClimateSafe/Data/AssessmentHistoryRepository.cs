using System;
using System.Collections.Generic;
using ClimateSafe.Auth;
using Npgsql;

namespace ClimateSafe
{
    internal sealed class AssessmentHistoryRepository
    {
        private readonly Func<NpgsqlConnection> createConnection;

        public AssessmentHistoryRepository(Func<NpgsqlConnection>? createConnection = null)
        {
            this.createConnection = createConnection ?? Database.Create;
        }

        public IReadOnlyList<AssessmentHistoryAnswer> GetAnswers(int userId, int assessmentId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            if (assessmentId <= 0) throw new ArgumentOutOfRangeException(nameof(assessmentId));

            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT aa.answer_id, COALESCE(aa.dimension_name, d.name),
                       COALESCE(aa.question_text, q.question_text), aa.value, aa.score
                FROM assessments a
                LEFT JOIN assessment_answers aa ON aa.assessment_id = a.assessment_id
                LEFT JOIN questions q ON q.question_id = aa.question_id
                LEFT JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
                WHERE a.user_id = @user AND a.assessment_id = @assessment
                ORDER BY q.dimension_id, q.question_id
                """, connection);
            command.Parameters.AddWithValue("user", userId);
            command.Parameters.AddWithValue("assessment", assessmentId);
            using var reader = command.ExecuteReader();
            var answers = new List<AssessmentHistoryAnswer>();
            bool sessionFound = false;
            while (reader.Read())
            {
                sessionFound = true;
                if (!reader.IsDBNull(0))
                    answers.Add(new AssessmentHistoryAnswer(reader.GetString(1), reader.GetString(2),
                        reader.GetString(3), reader.GetInt32(4)));
            }
            if (!sessionFound)
                throw new InvalidOperationException("Riwayat asesmen tidak ditemukan untuk pengguna ini.");
            return answers.AsReadOnly();
        }
    }
}
