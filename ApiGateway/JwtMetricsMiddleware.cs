using Prometheus;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ApiGateway;

public class JwtMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Counter TokenValidationTotal = Metrics.CreateCounter(
        "token_validation_total",
        "Total JWT validation attempts",
        new CounterConfiguration { LabelNames = new[] { "result" } });

    private static readonly Histogram JwtValidationDuration = Metrics.CreateHistogram(
        "jwt_validation_seconds",
        "Duration of JWT validation",
        new HistogramConfiguration { LabelNames = new[] { "result" } });

    public JwtMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var authorizeAttribute = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        var isProtected = authorizeAttribute != null;

        if (isProtected)
        {
            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            stopwatch.Stop();

            var result = context.Response.StatusCode == StatusCodes.Status401Unauthorized ? "failure" : "success";
            TokenValidationTotal.WithLabels(result).Inc();
            JwtValidationDuration.WithLabels(result).Observe(stopwatch.Elapsed.TotalSeconds);
        }
        else
        {
            await _next(context);
        }
    }
}