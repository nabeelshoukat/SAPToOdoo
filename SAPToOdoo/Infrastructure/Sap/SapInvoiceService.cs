using SAPToOdoo.Application.DTOs.Invoice;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SAPToOdoo.Infrastructure.Sap
{
    public sealed class SapInvoiceService : ISapInvoiceService
    {
        private static readonly Regex UdfName = new("^U_[A-Za-z0-9_]{1,30}$", RegexOptions.Compiled);

        private readonly ISapConnectionService _connectionService;
        private readonly ILogger<SapInvoiceService> _logger;

        public SapInvoiceService(ISapConnectionService connectionService, ILogger<SapInvoiceService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        public Task<InvoiceResponse> CreateAsync(InvoiceRequest request)
        {
            // Validate everything we can before opening a SAP connection.
            if (request.Lines is null || request.Lines.Count == 0)
                throw new SapException(ErrorCodes.SapOperationFailed, "Invoice must contain at least one line.");

            ValidateUdfNames(request.UserFields);
            foreach (var l in request.Lines) ValidateUdfNames(l.UserFields);

            return StaTaskRunner.RunAsync(() =>
            {
                using var handle = _connectionService.Connect();
                dynamic? invoice = null;
                dynamic? lines = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(request.NumAtCard) &&
                        InvoiceExists(handle, request.CardCode, request.NumAtCard!))
                    {
                        throw new SapException(ErrorCodes.SapOperationFailed,
                            $"An invoice for '{request.CardCode}' with reference '{request.NumAtCard}' already exists.");
                    }

                    invoice = handle.GetBusinessObject(SapObjectTypes.Invoices, "create invoice");

                    // ---- Header ----
                    invoice.DocType = 0;   // dDocument_Items
                    invoice.CardCode = request.CardCode;
                    invoice.DocDate = (request.DocDate ?? DateTime.Today).Date;
                    invoice.DocDueDate = (request.DocDueDate ?? request.DocDate ?? DateTime.Today).Date;
                    invoice.TaxDate = (request.TaxDate ?? request.DocDate ?? DateTime.Today).Date;

                    if (!string.IsNullOrWhiteSpace(request.NumAtCard)) invoice.NumAtCard = request.NumAtCard;
                    if (!string.IsNullOrWhiteSpace(request.Comments)) invoice.Comments = request.Comments;
                    if (!string.IsNullOrWhiteSpace(request.Currency)) invoice.DocCurrency = request.Currency;
                    if (request.SalesPersonCode.HasValue) invoice.SalesPersonCode = request.SalesPersonCode.Value;
                    if (request.Series.HasValue) invoice.Series = request.Series.Value;

                    SetUserFields(invoice.UserFields, request.UserFields);

                    // ---- Lines ----
                    lines = invoice.Lines;
                    for (int i = 0; i < request.Lines.Count; i++)
                    {
                        var l = request.Lines[i];

                        // A new document already has an empty line 0; add new rows only after it.
                        if (i > 0) lines.Add();
                        lines.SetCurrentLine(i);

                        lines.ItemCode = l.ItemCode;
                        lines.Quantity = l.Quantity;

                        if (l.UnitPrice.HasValue) lines.UnitPrice = l.UnitPrice.Value;
                        if (l.DiscountPercent.HasValue) lines.DiscountPercent = l.DiscountPercent.Value;
                        if (!string.IsNullOrWhiteSpace(l.WarehouseCode)) lines.WarehouseCode = l.WarehouseCode;
                        if (!string.IsNullOrWhiteSpace(l.TaxCode)) lines.TaxCode = l.TaxCode;

                        SetUserFields(lines.UserFields, l.UserFields);
                    }

                    // ---- Post ----
                    int result = invoice.Add();
                    if (result != 0)
                    {
                        int errorCode = handle.Company.GetLastErrorCode();
                        string errorMessage = handle.Company.GetLastErrorDescription();
                        throw new SapException(ErrorCodes.SapOperationFailed, errorMessage, errorCode);
                    }

                    // For documents, the new key is the DocEntry.
                    int docEntry = int.Parse(handle.Company.GetNewObjectKey());

                    // Reload to read the generated DocNum and totals.
                    if (!(bool)invoice.GetByKey(docEntry))
                    {
                        throw new SapException(ErrorCodes.SapOperationFailed,
                            $"Invoice was created (DocEntry {docEntry}) but could not be reloaded.");
                    }

                    _logger.LogInformation("SAP A/R Invoice created. DocEntry={DocEntry}, DocNum={DocNum}, Customer={CardCode}",
                        docEntry, (int)invoice.DocNum, request.CardCode);

                    return new InvoiceResponse
                    {
                        DocEntry = docEntry,
                        DocNum = invoice.DocNum,
                        CardCode = invoice.CardCode,
                        CardName = invoice.CardName,
                        DocDate = invoice.DocDate is DateTime d ? d : null,
                        Currency = invoice.DocCurrency,
                        DocTotal = Convert.ToDecimal(invoice.DocTotal),
                        VatSum = Convert.ToDecimal(invoice.VatSum)
                    };
                }
                catch (SapException) { throw; }
                catch (Exception ex)
                {
                    throw new SapException(ErrorCodes.SapOperationFailed,
                        $"Failed to create invoice for '{request.CardCode}' in SAP: {ex.Message}", innerException: ex);
                }
                finally
                {
                    if (lines is not null && Marshal.IsComObject(lines)) Marshal.FinalReleaseComObject(lines);
                    if (invoice is not null && Marshal.IsComObject(invoice)) Marshal.FinalReleaseComObject(invoice);
                }
            });
        }

        private static void ValidateUdfNames(Dictionary<string, object?>? udfs)
        {
            if (udfs is null) return;
            foreach (var name in udfs.Keys)
                if (!UdfName.IsMatch(name))
                    throw new SapException(ErrorCodes.SapOperationFailed, $"Invalid user field name '{name}'. Must look like U_Name.");
        }

        private static void SetUserFields(dynamic userFields, Dictionary<string, object?>? udfs)
        {
            if (udfs is null || udfs.Count == 0) return;

            dynamic? fields = null;
            try
            {
                fields = userFields.Fields;
                foreach (var (name, value) in udfs)
                {
                    dynamic? field = null;
                    try
                    {
                        field = fields.Item(name);
                        field.Value = NormalizeUdfValue(value);
                    }
                    finally
                    {
                        if (field is not null && Marshal.IsComObject(field)) Marshal.FinalReleaseComObject(field);
                    }
                }
            }
            finally
            {
                if (fields is not null && Marshal.IsComObject(fields)) Marshal.FinalReleaseComObject(fields);
                if (userFields is not null && Marshal.IsComObject(userFields)) Marshal.FinalReleaseComObject(userFields);
            }
        }

        private static object? NormalizeUdfValue(object? value)
        {
            if (value is System.Text.Json.JsonElement je)
            {
                return je.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => je.GetString(),
                    System.Text.Json.JsonValueKind.Number => je.TryGetInt32(out var i) ? i : je.GetDouble(),
                    System.Text.Json.JsonValueKind.True => "Y",
                    System.Text.Json.JsonValueKind.False => "N",
                    _ => null
                };
            }
            return value;
        }

        private static bool InvoiceExists(dynamic handle, string cardCode, string numAtCard)
        {
            dynamic? rs = null;
            try
            {
                rs = handle.GetBusinessObjectFromCandidates(SapObjectTypes.BoRecordsetCandidates, "check duplicate invoice");
                string query =
                    "SELECT TOP 1 DocEntry FROM OINV WITH (NOLOCK) " +
                    $"WHERE CardCode = '{cardCode.Replace("'", "''")}' " +
                    $"AND NumAtCard = '{numAtCard.Replace("'", "''")}' AND CANCELED = 'N'";
                rs.DoQuery(query);
                return !(bool)rs.EoF;
            }
            finally
            {
                if (rs is not null && Marshal.IsComObject(rs)) Marshal.FinalReleaseComObject(rs);
            }
        }
    }
}
