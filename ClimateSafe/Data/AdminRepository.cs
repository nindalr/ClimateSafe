using System;
using System.Data;
using Npgsql;

namespace ClimateSafe.Auth
{
    internal sealed class AdminRepository
    {
        private readonly Func<NpgsqlConnection> createConnection;

        public AdminRepository(Func<NpgsqlConnection>? createConnection = null)
        {
            this.createConnection = createConnection ?? Database.Create;
        }

        private T ExecuteAsAdmin<T>(int adminId, Func<NpgsqlConnection, NpgsqlTransaction, T> operation)
        {
            using var connection = createConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var authorize = new NpgsqlCommand("""
                SELECT 1 FROM users WHERE user_id = @id AND role = 'Admin' AND status = 'Active' FOR SHARE
                """, connection, transaction);
            authorize.Parameters.AddWithValue("id", adminId);
            if (authorize.ExecuteScalar() == null)
                throw new UnauthorizedAccessException("Akun admin tidak aktif atau tidak memiliki akses.");
            T result = operation(connection, transaction);
            transaction.Commit();
            return result;
        }

        public DataTable GetUsers(int adminId) => ExecuteAsAdmin(adminId, (connection, transaction) =>
        {
            using var command = new NpgsqlCommand("""
                SELECT user_id, name, email, role, status, created_at FROM users ORDER BY user_id
                """, connection, transaction);
            using var reader = command.ExecuteReader();
            var result = new DataTable();
            result.Load(reader);
            return result;
        });

        public bool UpdateName(int adminId, int userId, string name) =>
            Update(adminId, userId, "UPDATE users SET name = @value WHERE user_id = @id", name);

        public bool ResetPassword(int adminId, int userId, string passwordHash) =>
            Update(adminId, userId, "UPDATE users SET password_hash = @value WHERE user_id = @id", passwordHash);

        public string? ToggleStatus(int adminId, int userId) => ExecuteAsAdmin(adminId, (connection, transaction) =>
        {
            using var command = new NpgsqlCommand("""
                UPDATE users SET status = CASE status WHEN 'Active' THEN 'Inactive' ELSE 'Active' END
                WHERE user_id = @id AND status IN ('Active', 'Inactive') RETURNING status
                """, connection, transaction);
            command.Parameters.AddWithValue("id", userId);
            return command.ExecuteScalar() as string;
        });

        public bool DeleteUser(int adminId, int userId) => ExecuteAsAdmin(adminId, (connection, transaction) =>
        {
            using var command = new NpgsqlCommand("DELETE FROM users WHERE user_id = @id", connection, transaction);
            command.Parameters.AddWithValue("id", userId);
            return command.ExecuteNonQuery() == 1;
        });

        private bool Update(int adminId, int userId, string sql, string value) =>
            ExecuteAsAdmin(adminId, (connection, transaction) =>
            {
                using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("id", userId);
                command.Parameters.AddWithValue("value", value);
                return command.ExecuteNonQuery() == 1;
            });
    }
}
