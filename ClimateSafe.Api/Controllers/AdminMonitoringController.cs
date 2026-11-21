using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/monitoring")]
public sealed class AdminMonitoringController(AdminMonitoringRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MonitoringReport>> Get([FromQuery] string period = "today",
        [FromQuery] int? hazardId = null, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        int days = period switch { "today" => 1, "7days" => 7, "30days" => 30, _ => 0 };
        if (days == 0 || hazardId is <= 0 || page < 1)
            return BadRequest(new { message = "Periode, hazard, atau halaman tidak valid." });

        // WIB is UTC+7 year-round. Calendar days include today, up to the request time.
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var wibOffset = TimeSpan.FromHours(7);
        DateTime todayWib = now.ToOffset(wibOffset).Date;
        DateTimeOffset startUtc = new DateTimeOffset(todayWib.AddDays(1 - days), wibOffset).ToUniversalTime();
        return Ok(await repository.GetReportAsync(startUtc, now, hazardId, page, 50, ct));
    }
}
