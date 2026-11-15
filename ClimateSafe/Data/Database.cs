using Npgsql;

using System;
using System.IO;
using System.Linq;
using System.Globalization;

namespace ClimateSafe.Auth
{
    public static class Database
    {
        private const string PasswordVariable = "CLIMATESAFE_DB_PASSWORD";

        public static NpgsqlConnection Create()
        {
            string envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            var envValues = File.Exists(envPath)
                ? DotNetEnv.Env.NoEnvVars().Load(envPath).ToList()
                : new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();

            string? GetSetting(string name, string? fallback = null)
            {
                string? value = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrEmpty(value))
                    value = envValues.LastOrDefault(entry => entry.Key == name).Value;
                return value ?? fallback;
            }

            string? password = GetSetting(PasswordVariable);
            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException(
                    "Password database belum diatur. Isi CLIMATESAFE_DB_PASSWORD di .env lalu build ulang aplikasi.");
            }

            string? host = GetSetting("CLIMATESAFE_DB_HOST", "localhost");
            string? username = GetSetting("CLIMATESAFE_DB_USERNAME", "postgres");
            string? database = GetSetting("CLIMATESAFE_DB_NAME", "climatesafe");
            string? portText = GetSetting("CLIMATESAFE_DB_PORT", "5432");
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(database))
                throw new InvalidOperationException("Host, username, dan nama database tidak boleh kosong.");

            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out int port) ||
                port < 1 || port > 65535)
                throw new InvalidOperationException("CLIMATESAFE_DB_PORT harus berupa angka antara 1 dan 65535.");

            NpgsqlConnectionStringBuilder settings = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Username = username,
                Database = database,
                Password = password
            };
            return new NpgsqlConnection(settings.ConnectionString);
        }
    }
}



