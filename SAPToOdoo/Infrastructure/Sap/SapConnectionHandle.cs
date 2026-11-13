using System.Runtime.InteropServices;
using SAPToOdoo.Common;

namespace SAPToOdoo.Infrastructure.Sap;

/// <summary>
/// Owns a connected SAPbobsCOM.Company COM object for the lifetime of a single
/// operation. Disconnects and releases the COM object on dispose so nothing is
/// left hanging in memory.
/// </summary>
public sealed class SapConnectionHandle : IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    internal SapConnectionHandle(dynamic company, ILogger logger)
    {
        Company = company;
        _logger = logger;
    }

    public dynamic Company { get; }

    /// <summary>
    /// Wraps Company.GetBusinessObject with the null check the raw DI API call skips.
    /// SAP DI API returns null (not an exception) for an unsupported object type or a
    /// license that doesn't permit it, which otherwise surfaces later as a confusing
    /// "Cannot perform runtime binding on a null reference" error. Failing here instead
    /// captures SAP's own GetLastErrorCode/Description while it's still relevant.
    /// </summary>
    public dynamic GetBusinessObject(int objectType, string operationDescription)
    {
        if (Company is null || !(bool)Company.Connected)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                $"SAP DI API Company object is not connected while trying to {operationDescription}.");
        }

        dynamic? businessObject = Company.GetBusinessObject(objectType);
        if (businessObject is null)
        {
            int errorCode = Company.GetLastErrorCode();
            string errorMessage = Company.GetLastErrorDescription();
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                $"SAP DI API returned no object (type {objectType}) needed to {operationDescription}: " +
                $"[{errorCode}] {errorMessage}",
                errorCode);
        }

        return businessObject;
    }

    /// <summary>
    /// Tries each candidate BoObjectTypes value in order and returns the first one SAP
    /// accepts. Used only where the correct numeric value can't be confirmed ahead of
    /// time (e.g. BoRecordset varies across documented sources); on failure the
    /// exception lists SAP's own error for every candidate tried, so the real reason
    /// (wrong value vs. a license/authorization restriction) is visible in one round trip.
    /// </summary>
    public dynamic GetBusinessObjectFromCandidates(int[] objectTypeCandidates, string operationDescription)
    {
        if (Company is null || !(bool)Company.Connected)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                $"SAP DI API Company object is not connected while trying to {operationDescription}.");
        }

        var attempts = new List<string>();
        foreach (int objectType in objectTypeCandidates)
        {
            dynamic? businessObject = Company.GetBusinessObject(objectType);
            if (businessObject is not null)
            {
                return businessObject;
            }

            int errorCode = Company.GetLastErrorCode();
            string errorMessage = Company.GetLastErrorDescription();
            attempts.Add($"type {objectType}: [{errorCode}] {errorMessage}");
        }

        throw new SapException(
            ErrorCodes.SapOperationFailed,
            $"SAP DI API returned no object needed to {operationDescription} for any known object-type value. " +
            "Attempts: " + string.Join(" | ", attempts));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (Company is not null && (bool)Company.Connected)
            {
                Company.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disconnecting from SAP DI API.");
        }
        finally
        {
            if (Company is not null && Marshal.IsComObject(Company))
            {
                Marshal.FinalReleaseComObject(Company);
            }
        }
    }
}
