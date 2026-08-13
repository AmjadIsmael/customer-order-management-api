using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CustomerOrderManagement.API.RateLimiting;

public static class RateLimitingExtensions
{
    public const string AuthPolicyName = "auth";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
            ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => ToFixedWindowOptions(settings.Global)));

            // Stricter policy for the login endpoint to slow down credential-stuffing/brute-force attempts.
            options.AddPolicy(AuthPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => ToFixedWindowOptions(settings.Auth)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                };

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions ToFixedWindowOptions(RateLimitPolicySettings settings) =>
        new()
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = settings.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        };

    private static string GetClientKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}
