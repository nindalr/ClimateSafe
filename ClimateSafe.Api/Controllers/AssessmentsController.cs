using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assessments")]
public sealed class AssessmentsController(AssessmentHistoryRepository repository,
    AssessmentSubmissionRepository submissionRepository) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<AssessmentSubmissionResult>> Submit(AssessmentSubmissionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await submissionRepository.SubmitAsync(UserId, request, ct);
            return result.Created ? CreatedAtAction(nameof(GetDetail), new { id = result.AssessmentId }, result) : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (SubmissionConflictException) { return Conflict(new { message = "Kunci pengiriman sudah digunakan untuk asesmen lain." }); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException) { return Problem(statusCode: 503, title: "Konfigurasi asesmen belum tersedia."); }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentSummary>>> GetAll(CancellationToken ct) =>
        Ok(await repository.GetAllAsync(UserId, ct));

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<AssessmentSession>>> GetSessions(CancellationToken ct) =>
        Ok(await repository.GetSessionsAsync(UserId, ct));

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
