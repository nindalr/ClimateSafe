using System.Security.Claims;
using ClimateSafe.Api.Data;
using ClimateSafe.Api.Models;
using ClimateSafe.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimateSafe.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountsController(AccountRepository repository) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        string name = request.Name?.Trim() ?? "";
        string email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (name.Length == 0 || !email.Contains('@') || !email.Contains('.') ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new { message = "Nama, email, dan password minimal 6 karakter harus valid." });

        bool created = await repository.RegisterAsync(name, email, PasswordHasher.Hash(request.Password), ct);
        return created ? StatusCode(StatusCodes.Status201Created) : Conflict(new { message = "Email sudah terdaftar." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        string email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var stored = await repository.FindByEmailAsync(email, ct);
        if (stored == null || stored.Account.Status != "Active" || request.Password == null ||
            !PasswordHasher.Verify(request.Password, stored.PasswordHash))
            return Unauthorized(new { message = "Email atau password tidak valid, atau akun tidak aktif." });

        string token = SessionTokens.Create();
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddHours(8);
        if (!await repository.CreateSessionAsync(stored.Account.Id, stored.PasswordHash, SessionTokens.Hash(token), expires, ct))
            return Unauthorized(new { message = "Sesi tidak dapat dibuat. Silakan login kembali." });
        return Ok(new LoginResponse(token, expires, stored.Account));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<Account>> Me(CancellationToken ct)
    {
        string token = Request.Headers.Authorization.ToString()[7..].Trim();
        var account = await repository.FindBySessionAsync(SessionTokens.Hash(token), ct);
        return account == null ? Unauthorized() : Ok(account);
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<ActionResult<Account>> UpdateProfile(UpdateNameRequest request, CancellationToken ct)
    {
        string name = request.Name?.Trim() ?? "";
        if (name.Length == 0) return BadRequest(new { message = "Nama tidak boleh kosong." });
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var account = await repository.UpdateOwnNameAsync(id, name, ct);
        return account == null ? Unauthorized() : Ok(account);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string token = Request.Headers.Authorization.ToString()[7..].Trim();
        await repository.DeleteSessionAsync(SessionTokens.Hash(token), ct);
        return NoContent();
    }
}
