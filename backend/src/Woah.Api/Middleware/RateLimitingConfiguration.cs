using System.Threading.RateLimiting;

namespace Woah.Api.Middleware;

public static class RateLimitingConfiguration
{
    public const string SubmitAnswer = "submit-answer";
    public const string ItunesSearch = "itunes-search";

    public static IServiceCollection AddGameRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Too many requests. Please wait and try again." }, ct);
            };

            options.AddPolicy(SubmitAnswer, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(5),
                        QueueLimit = 0
                    }));

            options.AddPolicy(ItunesSearch, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}