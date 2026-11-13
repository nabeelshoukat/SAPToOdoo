using Microsoft.Extensions.Options;
using SAPToOdoo.Common;

namespace SAPToOdoo.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string HeaderName = "X-API-Key";

    // Health checks and API documentation stay reachable without a key so
    // infra monitoring and first-time setup don't require auth.
    private static readonly string[] ExemptPathPrefixes =
    [
        "/health",
        "/swagger",
        "/favicon.ico"
    ];

    private readonly RequestDelegate _next;
    private readonly AuthenticationOptions _options;

    public ApiKeyAuthMiddleware(RequestDelegate next, IOptions<AuthenticationOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ExemptPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            await WriteUnauthorized(context, "API key is not configured on the server.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
            !string.Equals(providedKey, _options.ApiKey, StringComparison.Ordinal))
        {
            await WriteUnauthorized(context, "Missing or invalid API key.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            new ApiError { Code = ErrorCodes.AuthenticationError, Message = message },
            context.GetCorrelationId());

        await context.Response.WriteAsJsonAsync(response);
    }
}
