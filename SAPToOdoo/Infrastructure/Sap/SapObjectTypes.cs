namespace SAPToOdoo.Infrastructure.Sap;

/// <summary>
/// Numeric values from SAPbobsCOM.BoObjectTypes / BoCardTypes.
/// Hard-coded because the DI API type library is only available on a server
/// where SAP Business One is installed, not on every build machine.
/// </summary>
internal static class SapObjectTypes
{
    public const int BusinessPartners = 2; // BoObjectTypes.oBusinessPartners
    public const int Items = 4; // BoObjectTypes.oItems
    public const int Warehouses = 64;
    public const int Invoices = 13; // oInvoices (A/R Invoice)
    public const int SalesTaxCodes = 128;
    /// <summary>
    /// The Items business object only supports single-key lookup (GetByKey) — it has
    /// no browse/list/sort capability. Recordset is the DI API's own object for
    /// read-only queries that master-data business objects can't express, and it runs
    /// entirely inside the already-connected Company session (not a separate SQL
    /// connection), so it's used here instead of adding direct database access.
    ///
    /// The exact BoRecordset numeric value isn't confirmed for this environment
    /// (cited differently across sources), so every plausible candidate is tried at
    /// runtime — see SapConnectionHandle.GetBusinessObjectFromCandidates.
    /// </summary>
    public static readonly int[] BoRecordsetCandidates = { 129, 300 };
}

internal static class SapCardTypes
{
    public const char Customer = 'C'; // BoCardTypes.cCustomer
}
