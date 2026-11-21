using System.Security.Claims;
using System.Text.Encodings.Web;
using ClimateSafe.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClimateSafe.Api.Services;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, AccountRepository repository)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();
        string token = header[7..].Trim();
        if (token.Length != 64 || !token.All(Uri.IsHexDigit))
            return AuthenticateResult.Fail("Token tidak valid.");
        var account = await repository.FindBySessionAsync(SessionTokens.Hash(token), Context.RequestAborted);
        if (account == null) return AuthenticateResult.Fail("Sesi tidak berlaku.");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Role, account.Role), new Claim(ClaimTypes.Name, account.Name) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
