using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Infrastructure.InMemory;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Integrations.Itunes;
using Woah.Api.Middleware;
using Woah.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));

builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ILobbyPlaylistService, LobbyPlaylistService>();
builder.Services.AddScoped<IAnswerNormalizer, AnswerNormalizer>();
builder.Services.AddScoped<IScoreCalculator, LinearScoreCalculator>();
builder.Services.AddSingleton<ILobbyCodeGenerator, LobbyCodeGenerator>();
builder.Services.AddSingleton<ILobbyPlaylistStore, InMemoryLobbyPlaylistStore>();

builder.Services.AddHttpClient<ItunesApiClient>(client =>
{
    client.BaseAddress = new Uri("https://itunes.apple.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoahDbContext>("db", tags: new[] { "ready" });

var app = builder.Build();

app.UseExceptionHandler();
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