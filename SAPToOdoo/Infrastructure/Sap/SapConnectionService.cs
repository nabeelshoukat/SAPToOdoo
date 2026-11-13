using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using SAPToOdoo.Common;

namespace SAPToOdoo.Infrastructure.Sap;

/// <summary>
/// Creates and connects the SAP DI API Company object.
/// Uses late-bound COM (ProgID lookup) instead of a compiled SAPbobsCOM interop
/// reference so the gateway builds on any machine and only requires the DI API
/// to be installed/registered on the machine it actually runs on.
/// </summary>
public sealed class SapConnectionService : ISapConnectionService
{
    private readonly SapOptions _options;
    private readonly ILogger<SapConnectionService> _logger;

    public SapConnectionService(IOptions<SapOptions> options, ILogger<SapConnectionService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public SapConnectionHandle Connect()
    {
        var companyType = Type.GetTypeFromProgID("SAPbobsCOM.Company");
        if (companyType is null)
        {
            throw new SapException(
                ErrorCodes.SapConnectionFailed,
                "SAP DI API is not installed or registered on this server (SAPbobsCOM.Company COM component not found).");
        }

        dynamic company;
        try
        {
            company = Activator.CreateInstance(companyType)!;
        }
        catch (Exception ex)
        {
            throw new SapException(
                ErrorCodes.SapConnectionFailed,
                "Unable to create the SAP DI API Company object.",
                innerException: ex);
        }

        company.Server = _options.Server;
        company.CompanyDB = _options.CompanyDb;
        company.UserName = _options.UserName;
        company.Password = _options.Password;
        company.DbServerType = _options.DbServerType;
        company.LicenseServer = _options.LicenseServer;
        // Only send a separate SQL login if one is configured.
        if (!string.IsNullOrWhiteSpace(_options.DbUserName))
        {
            company.DbUserName = _options.DbUserName;
            company.DbPassword = _options.DbPassword;
        }
        int result;
        try
        {
            result = company.Connect();
        }
        catch (Exception ex)
        {
            ReleaseCompany(company);
            throw new SapException(
                ErrorCodes.SapConnectionFailed,
                "SAP DI API connection attempt threw an exception.",
                innerException: ex);
        }

        if (result != 0)
        {
            int errorCode = result;
            string errorMessage = "Unable to connect to SAP Business One.";
            try
            {
                errorCode = company.GetLastErrorCode();
                errorMessage = company.GetLastErrorDescription();
            }
            catch
            {
                // Keep the fallback values above if the DI API cannot report a detailed error.
            }

            ReleaseCompany(company);
            throw new SapException(ErrorCodes.SapConnectionFailed, errorMessage, errorCode);
        }

        _logger.LogInformation("SAP DI API connection established (Server={Server}, CompanyDb={CompanyDb})",
            _options.Server, _options.CompanyDb);

        return new SapConnectionHandle(company, _logger);
    }

    private static void ReleaseCompany(object? company)
    {
        if (company is not null && Marshal.IsComObject(company))
        {
            Marshal.FinalReleaseComObject(company);
        }
    }
}
