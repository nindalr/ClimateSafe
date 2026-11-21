using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Route("api/hazards")]
public sealed class HazardsController(HazardRepository repository, QuestionCatalogRepository catalog,
    ILogger<HazardsController> logger) : ControllerBase
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

    [HttpGet("{id:int}/questions")]
    public async Task<ActionResult<IReadOnlyList<AssessmentQuestion>>> GetQuestions(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest(new { message = "ID hazard harus valid." });
        try
        {
            var questions = await catalog.GetQuestionsAsync(id, cancellationToken);
            return questions == null ? NotFound() : Ok(questions);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogError(ex, "Katalog pertanyaan tidak dapat dimuat dari database.");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Katalog pertanyaan belum tersedia.");
        }
    }
}
