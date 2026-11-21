using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Route("api/education")]
public sealed class EducationController(EducationContentRepository repository) : ControllerBase
{
    [HttpGet("hazards/{hazardId:int}")]
    public async Task<ActionResult<IReadOnlyList<PublishedEducationContent>>> Get(int hazardId, CancellationToken ct)
    {
        if (hazardId <= 0) return BadRequest(new { message = "ID hazard harus valid." });
        return Ok(await repository.GetPublishedAsync(hazardId, ct));
    }
}
