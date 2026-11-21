using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Route("api/hazards")]
public sealed class HazardsController(HazardRepository repository, ILogger<HazardsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Hazard>>> Get(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await repository.GetAllAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogError(ex, "Daftar hazard tidak dapat dimuat dari database.");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Data hazard belum tersedia.");
        }
    }
}
