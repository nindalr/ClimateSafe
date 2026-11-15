using System;
using Npgsql;
using System.Data;

namespace ClimateSafe.Auth
{
    public class UserRepository
    {
        public bool Register(string name, string email, string passwordHash)
        {
            using (NpgsqlConnection conn = Database.Create())
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(
                    "select * from st_register(@name, @email, @hash)", conn))
                {
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("email", email);
                    cmd.Parameters.AddWithValue("hash", passwordHash);
                    object? result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) == 1;
                }
            }
        }

        public UserAccount? GetByEmail(string email)
        {
            using (NpgsqlConnection conn = Database.Create())
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(
                    "select * from st_get_user(@email)", conn))
                {
                    cmd.Parameters.AddWithValue("email", email);
                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return null;

                        int id = rd.GetInt32(0);
                        string name = rd.GetString(1);
                        string mail = rd.GetString(2);
                        string hash = rd.GetString(3);
                        string role = rd.GetString(4);
                        string status = rd.GetString(5);
                        DateTime created = rd.GetDateTime(6);

                        if (role == "Admin")
                            return new Admin(id, name, mail, hash, role, status, created);
                        return new User(id, name, mail, hash, role, status, created);
                    }
                }
            }
        }

        public DataTable GetAllUsers()
        {
            using (NpgsqlConnection conn = Database.Create())
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from st_get_all_users()", conn))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(rd);
                    return dt;
                }
            }
        }

        public bool UpdateName(int userId, string name)
        {
            return ExecuteInt("select * from st_update_profile(@id, @name)",
                ("id", userId), ("name", name));
        }

        public bool SetStatus(int userId, string status)
        {
            return ExecuteInt("select * from st_set_status(@id, @status)",
                ("id", userId), ("status", status));
        }

        public bool DeleteUser(int userId)
        {
            return ExecuteInt("select * from st_delete_user(@id)", ("id", userId));
        }

        private bool ExecuteInt(string sql, params (string, object)[] parameters)
        {
            using (NpgsqlConnection conn = Database.Create())
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var p in parameters)
                        cmd.Parameters.AddWithValue(p.Item1, p.Item2);
                    object? result = cmd.ExecuteScalar();
                    return Convert.ToInt32(result) == 1;
                }
            }
        }
    }
}
