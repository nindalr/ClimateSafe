using System;
using System.Collections.Generic;
using ClimateSafe.Auth;
using Npgsql;

namespace ClimateSafe
{
    // Skor satu dimensi kesiapsiagaan pada sebuah asesmen, dalam persen (0..100).
    internal sealed record DimensionScore(int HazardId, int DimensionId, string DimensionName, double Score);

    internal sealed class RecommendationRepository
    {
        // Skor jawaban tertinggi per pertanyaan (Models/Answer: skala 0..3).
        private const double MaxAnswerScore = 3;

        private readonly Func<NpgsqlConnection> createConnection;

        public RecommendationRepository(Func<NpgsqlConnection>? createConnection = null)
        {
            this.createConnection = createConnection ?? Database.Create;
        }

        // Baca-saja: daftar kategori bahaya, urut hazard_id (dipakai halaman Edukasi).
        public IReadOnlyList<Hazard> GetHazards()
        {
            var hazards = new List<Hazard>();

            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand(
                "SELECT hazard_id, name FROM hazards ORDER BY hazard_id", connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                hazards.Add(new Hazard(reader.GetInt32(0), reader.GetString(1)));
            }
            return hazards.AsReadOnly();
        }

        // Baca-saja: skor per dimensi untuk satu asesmen milik pengguna tertentu.
        // Bila satu dimensi punya lebih dari satu pertanyaan, skornya dirata-ratakan.
        public IReadOnlyList<DimensionScore> GetDimensionScores(int userId, int assessmentId)
        {
            var totals = new List<DimensionScore>();
            var sums = new Dictionary<int, double>();
            var counts = new Dictionary<int, int>();
            var order = new List<int>();
            var names = new Dictionary<int, string>();
            var hazards = new Dictionary<int, int>();

            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT a.hazard_id, q.dimension_id, d.name, aa.score
                FROM assessments a
                JOIN assessment_answers aa ON aa.assessment_id = a.assessment_id
                JOIN questions q ON q.question_id = aa.question_id
                JOIN preparedness_dimensions d ON d.dimension_id = q.dimension_id
                WHERE a.assessment_id = @assessment AND a.user_id = @user
                ORDER BY q.dimension_id
                """, connection);
            command.Parameters.AddWithValue("assessment", assessmentId);
            command.Parameters.AddWithValue("user", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int hazardId = reader.GetInt32(0);
                int dimensionId = reader.GetInt32(1);
                string name = reader.GetString(2);
                double score = Convert.ToDouble(reader.GetValue(3)) * 100.0 / MaxAnswerScore;

                if (!counts.ContainsKey(dimensionId))
                {
                    order.Add(dimensionId);
                    names[dimensionId] = name;
                    hazards[dimensionId] = hazardId;
                    sums[dimensionId] = 0;
                    counts[dimensionId] = 0;
                }
                sums[dimensionId] += score;
                counts[dimensionId] += 1;
            }

            foreach (int dimensionId in order)
            {
                totals.Add(new DimensionScore(hazards[dimensionId], dimensionId,
                    names[dimensionId], sums[dimensionId] / counts[dimensionId]));
            }
            return totals;
        }

        // Baca-saja: nama setiap dimensi kesiapsiagaan, dipakai sebagai label tindakan checklist.
        public Dictionary<int, string> GetDimensionNames()
        {
            var names = new Dictionary<int, string>();

            using var connection = createConnection();
            connection.Open();
            using var command = new NpgsqlCommand(
                "SELECT dimension_id, name FROM preparedness_dimensions ORDER BY dimension_id", connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                names[reader.GetInt32(0)] = reader.GetString(1);
            }
            return names;
        }
    }
}
