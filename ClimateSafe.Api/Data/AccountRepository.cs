using ClimateSafe.Api.Models;
using Npgsql;

namespace ClimateSafe.Api.Data;

public sealed class AccountRepository(IConfiguration configuration)
{
    private NpgsqlConnection CreateConnection() => new(configuration.GetConnectionString("ClimateSafe")
        ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi."));

    private static Account ReadAccount(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetDateTime(5));

    public async Task<bool> RegisterAsync(string name, string email, string passwordHash, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO users (name, email, password_hash, role, status)
            VALUES (@name, @email, @hash, 'User', 'Active') ON CONFLICT DO NOTHING
            """, connection);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("hash", passwordHash);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    internal async Task<StoredAccount?> FindByEmailAsync(string email, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT user_id, name, email, role, status, created_at, password_hash
            FROM users WHERE lower(email) = @email
            """, connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new StoredAccount(ReadAccount(reader), reader.GetString(6)) : null;
    }

    public async Task<bool> CreateSessionAsync(int userId, string passwordHash, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var lockUser = new NpgsqlCommand("""
            SELECT 1 FROM users WHERE user_id = @id AND status = 'Active' AND password_hash = @hash FOR SHARE
            """, connection, transaction);
        lockUser.Parameters.AddWithValue("id", userId);
        lockUser.Parameters.AddWithValue("hash", passwordHash);
        if (await lockUser.ExecuteScalarAsync(ct) == null) return false;
        await using var command = new NpgsqlCommand("""
            INSERT INTO account_sessions (token_hash, user_id, expires_at)
            VALUES (@token, @id, @expires)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("token", tokenHash);
        command.Parameters.AddWithValue("expires", expiresAt);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<Account?> FindBySessionAsync(string tokenHash, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT u.user_id, u.name, u.email, u.role, u.status, u.created_at
            FROM account_sessions s JOIN users u ON u.user_id = s.user_id
            WHERE s.token_hash = @token AND s.expires_at > CURRENT_TIMESTAMP AND u.status = 'Active'
            """, connection);
        command.Parameters.AddWithValue("token", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadAccount(reader) : null;
    }

    public async Task DeleteSessionAsync(string tokenHash, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("DELETE FROM account_sessions WHERE token_hash = @token", connection);
        command.Parameters.AddWithValue("token", tokenHash);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Account?> UpdateOwnNameAsync(int id, string name, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE users SET name = @name WHERE user_id = @id AND status = 'Active'
            RETURNING user_id, name, email, role, status, created_at
            """, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadAccount(reader) : null;
    }

    public async Task<IReadOnlyList<Account>> GetUsersAsync(CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT user_id, name, email, role, status, created_at FROM users ORDER BY user_id", connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var users = new List<Account>();
        while (await reader.ReadAsync(ct)) users.Add(ReadAccount(reader));
        return users;
    }

    public async Task<bool> AdminChangeAsync(int actorId, int targetId, string operation, string? value, CancellationToken ct)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var authorize = new NpgsqlCommand(
            "SELECT 1 FROM users WHERE user_id = @id AND role = 'Admin' AND status = 'Active' FOR SHARE", connection, transaction);
        authorize.Parameters.AddWithValue("id", actorId);
        if (await authorize.ExecuteScalarAsync(ct) == null) throw new UnauthorizedAccessException();
        string sql = operation switch
        {
            "name" => "UPDATE users SET name = @value WHERE user_id = @id",
            "password" => "UPDATE users SET password_hash = @value WHERE user_id = @id",
            "status" => "UPDATE users SET status = CASE status WHEN 'Active' THEN 'Inactive' ELSE 'Active' END WHERE user_id = @id AND status IN ('Active', 'Inactive')",
            "delete" => "DELETE FROM users WHERE user_id = @id",
            _ => throw new ArgumentException("Operasi tidak dikenal.")
        };
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", targetId);
        if (value != null) command.Parameters.AddWithValue("value", value);
        bool changed = await command.ExecuteNonQueryAsync(ct) == 1;
        if (changed && (operation == "password" || operation == "status"))
        {
            await using var revoke = new NpgsqlCommand("DELETE FROM account_sessions WHERE user_id = @id", connection, transaction);
            revoke.Parameters.AddWithValue("id", targetId);
            await revoke.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return changed;
    }
}
