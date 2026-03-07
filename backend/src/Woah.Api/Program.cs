using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Infrastructure;
using Woah.Api.Infrastructure.Models;
using Woah.Api.Spotify;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));

builder.Services
    .AddOptions<SpotifyOptions>()
    .Bind(builder.Configuration.GetSection(SpotifyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<SpotifyAuthService>();
builder.Services.AddHttpClient<SpotifyApiClient>();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();


app.Run();