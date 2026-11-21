using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using ClimateSafe.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(AccountRepository repository) : ControllerBase
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Account>>> GetUsers(CancellationToken ct) =>
        Ok(await repository.GetUsersAsync(ct));

    [HttpPatch("{id:int}/name")]
    public async Task<IActionResult> UpdateName(int id, UpdateNameRequest request, CancellationToken ct)
    {
        string name = request.Name?.Trim() ?? "";
        if (id <= 0 || name.Length == 0) return BadRequest(new { message = "ID dan nama harus valid." });
        return await Change(id, "name", name, ct);
    }

    [HttpPost("{id:int}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        if (id <= 0 || id == ActorId) return BadRequest(new { message = "Status akun sendiri tidak dapat diubah." });
        return await Change(id, "status", null, ct);
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new { message = "Password minimal 6 karakter dan tidak boleh kosong." });
        return await Change(id, "password", PasswordHasher.Hash(request.Password), ct);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id <= 0 || id == ActorId) return BadRequest(new { message = "Akun sendiri tidak dapat dihapus." });
        return await Change(id, "delete", null, ct);
    }

    private async Task<IActionResult> Change(int id, string operation, string? value, CancellationToken ct)
    {
        try { return await repository.AdminChangeAsync(ActorId, id, operation, value, ct) ? NoContent() : NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
