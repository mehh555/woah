using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();

var connStr = builder.Configuration.GetConnectionString("WoahDb");
if (string.IsNullOrWhiteSpace(connStr))
    throw new InvalidOperationException("Brak connection stringa: ConnectionStrings:WoahDb (User Secrets / appsettings / env).");

builder.Services.AddDbContext<WoahDbContext>(options =>
{
    options.UseNpgsql(connStr, npgsql =>
        npgsql.MigrationsAssembly(typeof(WoahDbContext).Assembly.FullName));
});

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoahDbContext>("db", tags: new[] { "ready" })
    .AddCheck("ready", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.Run();