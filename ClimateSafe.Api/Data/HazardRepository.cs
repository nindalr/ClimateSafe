using ClimateSafe.Api.Models;
using Npgsql;

namespace ClimateSafe.Api.Data;

public sealed class HazardRepository(IConfiguration configuration)
{
    public async Task<IReadOnlyList<Hazard>> GetAllAsync(CancellationToken cancellationToken)
    {
        string connectionString = configuration.GetConnectionString("ClimateSafe")
            ?? throw new InvalidOperationException("ConnectionStrings:ClimateSafe belum dikonfigurasi.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT hazard_id, name FROM hazards ORDER BY hazard_id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var hazards = new List<Hazard>();
        while (await reader.ReadAsync(cancellationToken))
            hazards.Add(new Hazard(reader.GetInt32(0), reader.GetString(1)));

        return hazards;
    }
}
