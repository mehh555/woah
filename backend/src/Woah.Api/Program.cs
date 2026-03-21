using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Hubs;
using Woah.Api.Infrastructure.InMemory;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Integrations.Itunes;
using Woah.Api.Middleware;
using Woah.Api.Services.Lobby;
using Woah.Api.Services.Notifications;
using Woah.Api.Services.Playlist;
using Woah.Api.Services.Session;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));

builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ILobbyPlaylistService, LobbyPlaylistService>();
builder.Services.AddScoped<IAnswerNormalizer, AnswerNormalizer>();
builder.Services.AddScoped<IScoreCalculator, LinearScoreCalculator>();
builder.Services.AddScoped<ISessionProgressEngine, SessionProgressEngine>();
builder.Services.AddScoped<ISessionStateBuilder, SessionStateBuilder>();
builder.Services.AddScoped<IGameNotifier, GameNotifier>();
builder.Services.AddSingleton<ILobbyCodeGenerator, LobbyCodeGenerator>();
builder.Services.AddSingleton<ILobbyPlaylistStore, InMemoryLobbyPlaylistStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ItunesApiClient>(client =>
{
    client.BaseAddress = new Uri("https://itunes.apple.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoahDbContext>("db", tags: new[] { "ready" });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var hasBuiltFrontend = Directory.Exists(webRootPath) &&
                       Directory.EnumerateFiles(webRootPath, "*", SearchOption.AllDirectories).Any();

if (hasBuiltFrontend)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready")
});

app.Run();