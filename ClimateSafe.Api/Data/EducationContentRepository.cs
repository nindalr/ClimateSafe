using ClimateSafe.Api.Models;
using Npgsql;
using NpgsqlTypes;

namespace ClimateSafe.Api.Data;

public sealed class EducationContentRepository(IConfiguration configuration)
{
    private NpgsqlConnection CreateConnection() => new(configuration.GetConnectionString("ClimateSafe")
        ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi."));

    private const string AdminColumns = "content_id, hazard_id, title, body, source_name, source_url, published_at, created_at, updated_at, updated_by, published_title, published_body, published_source_name, published_source_url";

    private static AdminEducationContent ReadAdmin(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
        !reader.IsDBNull(6), reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        reader.GetDateTime(7), reader.GetDateTime(8), reader.IsDBNull(9) ? null : reader.GetInt32(9),
        reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11),
        reader.IsDBNull(12) ? null : reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetString(13));

    public async Task<IReadOnlyList<PublishedEducationContent>> GetPublishedAsync(int hazardId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT content_id, hazard_id, published_title, published_body, published_source_name, published_source_url, published_at
            FROM education_contents WHERE hazard_id = @hazard AND published_at IS NOT NULL ORDER BY content_id
            """, connection);
        command.Parameters.AddWithValue("hazard", hazardId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<PublishedEducationContent>();
        while (await reader.ReadAsync(ct))
            result.Add(new PublishedEducationContent(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetDateTime(6)));
        return result;
    }

    public async Task<IReadOnlyList<AdminEducationContent>> GetAllAsync(int? hazardId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand($"SELECT {AdminColumns} FROM education_contents WHERE (@hazard IS NULL OR hazard_id = @hazard) ORDER BY hazard_id, content_id", connection);
        command.Parameters.Add("hazard", NpgsqlDbType.Integer).Value = (object?)hazardId ?? DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<AdminEducationContent>();
        while (await reader.ReadAsync(ct)) result.Add(ReadAdmin(reader));
        return result;
    }

    public async Task<AdminEducationContent?> GetByIdAsync(int contentId, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand($"SELECT {AdminColumns} FROM education_contents WHERE content_id = @id", connection);
        command.Parameters.AddWithValue("id", contentId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadAdmin(reader) : null;
    }

    public async Task<AdminEducationContent?> SaveDraftAsync(int actorId, int? contentId, int? hazardId,
        string title, string body, string? sourceName, string? sourceUrl, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await EnsureAdminAsync(connection, transaction, actorId, ct);
        string sql = contentId == null
            ? $"INSERT INTO education_contents (hazard_id, title, body, source_name, source_url, updated_by) SELECT hazard_id, @title, @body, @source_name, @source_url, @actor FROM hazards WHERE hazard_id = @hazard RETURNING {AdminColumns}"
            : $"UPDATE education_contents SET title = @title, body = @body, source_name = @source_name, source_url = @source_url, updated_by = @actor, updated_at = CURRENT_TIMESTAMP WHERE content_id = @id RETURNING {AdminColumns}";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        if (contentId.HasValue) command.Parameters.AddWithValue("id", contentId.Value);
        else command.Parameters.AddWithValue("hazard", hazardId!.Value);
        command.Parameters.AddWithValue("actor", actorId);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("body", body);
        command.Parameters.Add("source_name", NpgsqlDbType.Text).Value = (object?)sourceName ?? DBNull.Value;
        command.Parameters.Add("source_url", NpgsqlDbType.Text).Value = (object?)sourceUrl ?? DBNull.Value;
        AdminEducationContent? result;
        await using (var reader = await command.ExecuteReaderAsync(ct))
            result = await reader.ReadAsync(ct) ? ReadAdmin(reader) : null;
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<AdminEducationContent?> SetPublishedAsync(int actorId, int contentId, bool publish, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await EnsureAdminAsync(connection, transaction, actorId, ct);
        string values = publish
            ? "published_title = title, published_body = body, published_source_name = source_name, published_source_url = source_url, published_at = CURRENT_TIMESTAMP"
            : "published_title = NULL, published_body = NULL, published_source_name = NULL, published_source_url = NULL, published_at = NULL";
        await using var command = new NpgsqlCommand($"""
            UPDATE education_contents SET {values}, updated_at = CURRENT_TIMESTAMP, updated_by = @actor
            WHERE content_id = @id RETURNING {AdminColumns}
            """, connection, transaction);
        command.Parameters.AddWithValue("id", contentId);
        command.Parameters.AddWithValue("actor", actorId);
        AdminEducationContent? result;
        await using (var reader = await command.ExecuteReaderAsync(ct))
            result = await reader.ReadAsync(ct) ? ReadAdmin(reader) : null;
        await transaction.CommitAsync(ct);
        return result;
    }

    private static async Task EnsureAdminAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int actorId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT 1 FROM users WHERE user_id = @id AND role = 'Admin' AND status = 'Active' FOR SHARE
            """, connection, transaction);
        command.Parameters.AddWithValue("id", actorId);
        if (await command.ExecuteScalarAsync(ct) == null) throw new UnauthorizedAccessException();
    }
}
