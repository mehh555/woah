using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Woah.Api.Infrastructure.WoahDbContext;
using Woah.Api.Services;
using Woah.Api.Infrastructure.Auth;
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();

builder.Services.AddDbContext<WoahDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WoahDb")));


builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SpotifyOAuthService>();
builder.Services.AddHttpClient<SpotifyOAuthService>();



var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();


app.Run();