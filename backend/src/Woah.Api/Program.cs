using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Hubs;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Integrations.Itunes;
using Woah.Api.Middleware;
using Woah.Api.Services.Cleanup;
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
builder.Services.AddGameRateLimiting();

var allowedOrigins = builder.Configuration
    .GetSection("AllowedCorsOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ILobbyPlaylistService, LobbyPlaylistService>();
builder.Services.AddScoped<IAnswerSubmissionHandler, AnswerSubmissionHandler>();
builder.Services.AddSingleton<IAnswerNormalizer, AnswerNormalizer>();
builder.Services.AddSingleton<IAnswerEvaluator, AnswerEvaluator>();
builder.Services.AddSingleton<IScoreCalculator, LinearScoreCalculator>();
builder.Services.AddScoped<ISessionFactory, SessionFactory>();
builder.Services.AddScoped<ISessionStartValidator, SessionStartValidator>();
builder.Services.AddScoped<ISessionProgressEngine, SessionProgressEngine>();
builder.Services.AddScoped<ISessionStateBuilder, SessionStateBuilder>();
builder.Services.AddScoped<IGameNotifier, GameNotifier>();
builder.Services.AddSingleton<ILobbyCodeGenerator, LobbyCodeGenerator>();
builder.Services.AddHostedService<StaleGameCleanupService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ItunesSettings>(builder.Configuration.GetSection(ItunesSettings.SectionName));
builder.Services.AddHttpClient<ItunesApiClient>(client =>
{
    client.BaseAddress = new Uri("https://itunes.apple.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<WoahDbContext>("db", tags: new[] { "ready" });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WoahDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
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