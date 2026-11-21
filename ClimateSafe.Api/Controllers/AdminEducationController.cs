using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/education")]
public sealed class AdminEducationController(EducationContentRepository repository) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminEducationContent>>> GetAll([FromQuery] int? hazardId, CancellationToken ct)
    {
        if (hazardId.HasValue && hazardId.Value <= 0) return BadRequest(new { message = "ID hazard harus valid." });
        return Ok(await repository.GetAllAsync(hazardId, ct));
    }

    [HttpGet("{contentId:int}")]
    public async Task<ActionResult<AdminEducationContent>> Get(int contentId, CancellationToken ct)
    {
        if (contentId <= 0) return BadRequest(new { message = "ID materi harus valid." });
        var content = await repository.GetByIdAsync(contentId, ct);
        return content == null ? NotFound() : Ok(content);
    }

    [HttpPost]
    public Task<ActionResult<AdminEducationContent>> Create(CreateEducationContentRequest request, CancellationToken ct) =>
        Save(null, request.HazardId, new(request.Title, request.Body, request.SourceName, request.SourceUrl), ct);

    [HttpPut("{contentId:int}")]
    public Task<ActionResult<AdminEducationContent>> Update(int contentId, EducationContentRequest request, CancellationToken ct) =>
        Save(contentId, null, request, ct);

    private async Task<ActionResult<AdminEducationContent>> Save(int? contentId, int? hazardId, EducationContentRequest request, CancellationToken ct)
    {
        string title = request.Title?.Trim() ?? "";
        string body = request.Body?.Trim() ?? "";
        string? sourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim();
        string? sourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim();
        if ((contentId.HasValue && contentId.Value <= 0) || (hazardId.HasValue && hazardId.Value <= 0) || title.Length == 0 || body.Length == 0)
            return BadRequest(new { message = "ID hazard, judul, dan isi materi harus valid." });
        if (sourceUrl != null && (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
                                  (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return BadRequest(new { message = "Tautan sumber harus berupa URL HTTP atau HTTPS yang valid." });
        try
        {
            var content = await repository.SaveDraftAsync(ActorId, contentId, hazardId, title, body, sourceName, sourceUrl, ct);
            return content == null ? NotFound() : Ok(content);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{contentId:int}/publish")]
    public Task<ActionResult<AdminEducationContent>> Publish(int contentId, CancellationToken ct) => SetPublished(contentId, true, ct);

    [HttpPost("{contentId:int}/unpublish")]
    public Task<ActionResult<AdminEducationContent>> Unpublish(int contentId, CancellationToken ct) => SetPublished(contentId, false, ct);

    private async Task<ActionResult<AdminEducationContent>> SetPublished(int contentId, bool publish, CancellationToken ct)
    {
        if (contentId <= 0) return BadRequest(new { message = "ID materi harus valid." });
        try
        {
            var content = await repository.SetPublishedAsync(ActorId, contentId, publish, ct);
            return content == null ? NotFound() : Ok(content);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
