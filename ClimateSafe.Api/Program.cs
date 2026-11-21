var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ClimateSafe.Api.Data.HazardRepository>();
builder.Services.AddSingleton<ClimateSafe.Api.Data.AccountRepository>();
builder.Services.AddAuthentication("Session")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ClimateSafe.Api.Services.SessionAuthenticationHandler>("Session", null);
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
