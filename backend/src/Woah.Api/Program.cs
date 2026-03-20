using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));

builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddSingleton<ILobbyCodeGenerator, LobbyCodeGenerator>();

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoahDbContext>("db", tags: new[] { "ready" });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();   

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();