using SAPToOdoo.Common;
using SAPToOdoo.Infrastructure.Sap;

namespace SAPToOdoo.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (SapException ex)
        {
            await WriteError(context, ex.Code, ex.Message, MapStatusCode(ex.Code), ex.SapErrorCode, ex);
        }
        catch (Exception ex)
        {
            await WriteError(context, ErrorCodes.InternalError, "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError, null, ex);
        }
    }

    private async Task WriteError(HttpContext context, string code, string message, int statusCode,
        int? sapErrorCode, Exception ex)
    {
        var correlationId = context.GetCorrelationId();

        _logger.LogError(ex, "Request failed with {ErrorCode}: {Message}", code, message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            new ApiError { Code = code, Message = message, SapErrorCode = sapErrorCode },
            correlationId);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static int MapStatusCode(string code) => code switch
    {
        ErrorCodes.SapConnectionFailed => StatusCodes.Status503ServiceUnavailable,
        ErrorCodes.SapOperationFailed => StatusCodes.Status502BadGateway,
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.ValidationError => StatusCodes.Status400BadRequest,
        ErrorCodes.AuthenticationError => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError
    };
}
