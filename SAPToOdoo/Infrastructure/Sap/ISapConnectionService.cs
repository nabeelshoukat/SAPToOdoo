namespace SAPToOdoo.Infrastructure.Sap;

public interface ISapConnectionService
{
    /// <summary>
    /// Connects to SAP Business One via the DI API and returns a handle that owns the
    /// underlying COM object. Must be called from an STA thread (see StaTaskRunner) and
    /// disposed to release the connection and COM resources.
    /// </summary>
    SapConnectionHandle Connect();
}
