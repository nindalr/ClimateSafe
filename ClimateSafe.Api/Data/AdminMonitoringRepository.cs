using System.Data;
using ClimateSafe.Api.Models;
using Npgsql;
using NpgsqlTypes;

namespace ClimateSafe.Api.Data;

public sealed class AdminMonitoringRepository(IConfiguration configuration)
{
    public async Task<MonitoringReport> GetReportAsync(DateTimeOffset startUtc, DateTimeOffset endUtc,
        int? hazardId, int page, int pageSize, CancellationToken ct)
    {
        string connectionString = configuration.GetConnectionString("ClimateSafe")
            ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        long totalUsers, activeUsers;
        await using (var command = new NpgsqlCommand("""
            SELECT count(*), count(*) FILTER (WHERE status = 'Active') FROM users WHERE role = 'User'
            """, connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            await reader.ReadAsync(ct);
            totalUsers = reader.GetInt64(0);
            activeUsers = reader.GetInt64(1);
        }

        const string filter = "a.created_at >= @start AND a.created_at < @end AND (@hazard IS NULL OR a.hazard_id = @hazard)";
        void AddFilters(NpgsqlCommand command)
        {
            command.Parameters.AddWithValue("start", startUtc);
            command.Parameters.AddWithValue("end", endUtc);
            command.Parameters.Add("hazard", NpgsqlDbType.Integer).Value = (object?)hazardId ?? DBNull.Value;
        }

        long totalAssessments;
        await using (var command = new NpgsqlCommand($"SELECT count(*) FROM assessments a WHERE {filter}", connection, transaction))
        {
            AddFilters(command);
            totalAssessments = (long)(await command.ExecuteScalarAsync(ct))!;
        }

        var assessments = new List<MonitoringAssessment>();
        await using (var command = new NpgsqlCommand($"""
            SELECT a.assessment_id, a.created_at, u.name, h.name, a.overall_score, a.level
            FROM assessments a JOIN users u ON u.user_id = a.user_id
            JOIN hazards h ON h.hazard_id = a.hazard_id
            WHERE {filter}
            ORDER BY a.created_at DESC, a.assessment_id DESC LIMIT @limit OFFSET @offset
            """, connection, transaction))
        {
            AddFilters(command);
            command.Parameters.AddWithValue("limit", pageSize);
            command.Parameters.AddWithValue("offset", ((long)page - 1) * pageSize);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                assessments.Add(new MonitoringAssessment(reader.GetInt32(0), reader.GetDateTime(1),
                    reader.GetString(2), reader.GetString(3), reader.GetDouble(4), reader.GetString(5)));
        }
        await transaction.CommitAsync(ct);
        return new MonitoringReport(totalUsers, activeUsers, totalAssessments, page, pageSize, startUtc, endUtc, assessments);
    }
}
