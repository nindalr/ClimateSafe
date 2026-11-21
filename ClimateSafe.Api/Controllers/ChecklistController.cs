using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/checklist")]
public sealed class ChecklistController(AssessmentHistoryRepository repository) : ControllerBase
{
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<ChecklistItem>> SetCompleted(int id, UpdateChecklistRequest request, CancellationToken ct)
    {
        if (id <= 0 || request.IsCompleted == null)
            return BadRequest(new { message = "ID dan status checklist harus valid." });
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await repository.SetCompletedAsync(userId, id, request.IsCompleted.Value, ct);
        return item == null ? NotFound() : Ok(item);
    }
}
