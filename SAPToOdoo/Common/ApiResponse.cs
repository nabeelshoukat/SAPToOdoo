namespace SAPToOdoo.Common;

public class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public int? SapErrorCode { get; init; }
}

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data, string correlationId, string? message = null) => new()
    {
        Success = true,
        Data = data,
        CorrelationId = correlationId,
        Message = message
    };

    public static ApiResponse<T> Fail(ApiError error, string correlationId) => new()
    {
        Success = false,
        Error = error,
        CorrelationId = correlationId
    };
}
