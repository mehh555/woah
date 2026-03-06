using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
<<<<<<< HEAD
using Woah.Api.Infrastructure;
using Woah.Api.Infrastructure.Models;
using Woah.Api.Spotify;

=======
using Woah.Api.Infrastructure.WoahDbContext;
using Woah.Api.Services;
using Woah.Api.Infrastructure.Auth;
>>>>>>> c55dddf1a83471d95a66370076cc2c34ab93b14e
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

<<<<<<< HEAD
builder.Services.AddHttpClient<SpotifyAuthService>();
builder.Services.AddHttpClient<SpotifyApiClient>();
=======
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SpotifyOAuthService>();
builder.Services.AddHttpClient<SpotifyOAuthService>();

>>>>>>> c55dddf1a83471d95a66370076cc2c34ab93b14e


var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();


app.Run();