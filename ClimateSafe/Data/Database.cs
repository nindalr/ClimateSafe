using Npgsql;

namespace ClimateSafe.Auth
{
    public static class Database
    {
        private const string ConnString =
            "Host=localhost;Port=5432;Username=postgres;Password=GANTI_PASSWORD_ANDA;Database=climatesafe";

        public static NpgsqlConnection Create()
        {
            return new NpgsqlConnection(ConnString);
        }
    }
}



