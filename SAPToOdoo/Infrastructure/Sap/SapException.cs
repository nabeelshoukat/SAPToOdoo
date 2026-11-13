namespace SAPToOdoo.Infrastructure.Sap;

public class SapException : Exception
{
    public string Code { get; }
    public int? SapErrorCode { get; }

    public SapException(string code, string message, int? sapErrorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        SapErrorCode = sapErrorCode;
    }
}
