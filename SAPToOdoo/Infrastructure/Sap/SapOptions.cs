namespace SAPToOdoo.Infrastructure.Sap;

public class SapOptions
{
    public string Server { get; set; } = string.Empty;
    public string CompanyDb { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int DbServerType { get; set; }
    public string DbUserName { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public string LicenseServer { get; set; } = string.Empty;
}
