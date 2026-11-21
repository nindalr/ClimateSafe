namespace ClimateSafe.Api.Models;

public sealed record Account(int Id, string Name, string Email, string Role, string Status, DateTime CreatedAt);
public sealed record RegisterRequest(string? Name, string? Email, string? Password);
public sealed record LoginRequest(string? Email, string? Password);
public sealed record UpdateNameRequest(string? Name);
public sealed record ResetPasswordRequest(string? Password);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, Account Account);

internal sealed record StoredAccount(Account Account, string PasswordHash);
