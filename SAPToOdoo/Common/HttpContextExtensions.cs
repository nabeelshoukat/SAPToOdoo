namespace SAPToOdoo.Common;

public static class HttpContextExtensions
{
    public const string CorrelationIdItemKey = "CorrelationId";

    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdItemKey, out var value) && value is string correlationId
            ? correlationId
            : string.Empty;
}
