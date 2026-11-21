using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assessments")]
public sealed class AssessmentsController(AssessmentHistoryRepository repository) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentSummary>>> GetAll(CancellationToken ct) =>
        Ok(await repository.GetAllAsync(UserId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssessmentDetail>> GetDetail(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest(new { message = "ID asesmen harus valid." });
        var detail = await repository.GetDetailAsync(UserId, id, ct);
        return detail == null ? NotFound() : Ok(detail);
    }

    [HttpGet("{id:int}/checklist")]
    public async Task<ActionResult<IReadOnlyList<ChecklistItem>>> GetChecklist(int id, CancellationToken ct)
    {
        if (id <= 0) return BadRequest(new { message = "ID asesmen harus valid." });
        var detail = await repository.GetDetailAsync(UserId, id, ct);
        return detail == null ? NotFound() : Ok(detail.Checklist);
    }
}
