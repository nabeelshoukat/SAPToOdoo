using SAPToOdoo.Application.DTOs.Taxes;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using System.Runtime.InteropServices;

namespace SAPToOdoo.Infrastructure.Sap
{
    public sealed class SapTaxService : ISapTaxService
    {
        private readonly ISapConnectionService _connectionService;
        private readonly ILogger<SapTaxService> _logger;

        public SapTaxService(
            ISapConnectionService connectionService,
            ILogger<SapTaxService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        public Task<IReadOnlyList<TaxResponse>> GetAsync(
            string? search,
            string? code,
            string? name,
            decimal? rate,
            string? validForAR,
            string? validForAP,
            string? locked,
            int limit)
        {
            return StaTaskRunner.RunAsync<IReadOnlyList<TaxResponse>>(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? recordSet = null;

                try
                {
                    if (limit < 1)
                        limit = 20;

                    if (limit > 100)
                        limit = 100;

                    recordSet =
                        handle.GetBusinessObjectFromCandidates(
                            SapObjectTypes.BoRecordsetCandidates,
                            "get tax codes");

                    var where = new List<string>();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        string value =
                            EscapeSql(search);

                        where.Add($@"
                        (
                            Code LIKE '%{value}%'
                            OR Name LIKE '%{value}%'
                        )");
                    }

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        string value =
                            EscapeSql(code);

                        where.Add(
                            $"Code = '{value}'");
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        string value =
                            EscapeSql(name);

                        where.Add(
                            $"Name LIKE '%{value}%'");
                    }

                    if (rate.HasValue)
                    {
                        where.Add(
                            $"Rate = {rate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }

                    if (!string.IsNullOrWhiteSpace(validForAR))
                    {
                        string value =
                            EscapeSql(validForAR);

                        where.Add(
                            $"ValidForAR = '{value}'");
                    }

                    if (!string.IsNullOrWhiteSpace(validForAP))
                    {
                        string value =
                            EscapeSql(validForAP);

                        where.Add(
                            $"ValidForAP = '{value}'");
                    }

                    if (!string.IsNullOrWhiteSpace(locked))
                    {
                        string value =
                            EscapeSql(locked);

                        where.Add(
                            $"Lock = '{value}'");
                    }

                    string whereSql =
                        where.Count > 0
                            ? "WHERE " + string.Join(" AND ", where)
                            : string.Empty;

                    string query = $@"
                    SELECT TOP {limit}
                        Code,
                        Name,
                        Rate,
                        Freight,
                        UserSign,
                        ValidForAR,
                        ValidForAP,
                        TfcId,
                        Lock,
                        TaxIcms,
                        IsItmLevel,
                        CfopIn,
                        CfopOut,
                        LogInstanc,
                        UserSign2,
                        UpdateDate,
                        FADebit
                    FROM OSTC
                    {whereSql}
                    ORDER BY Code";

                    recordSet.DoQuery(query);

                    var result =
                        new List<TaxResponse>();

                    if (!(bool)recordSet.EoF)
                    {
                        recordSet.MoveFirst();

                        while (!(bool)recordSet.EoF)
                        {
                            result.Add(new TaxResponse
                            {
                                Code =
                                    recordSet.Fields.Item("Code")
                                        .Value?.ToString()
                                    ?? string.Empty,

                                Name =
                                    recordSet.Fields.Item("Name")
                                        .Value?.ToString()
                                    ?? string.Empty,

                                Rate =
                                    Convert.ToDecimal(
                                        recordSet.Fields.Item("Rate").Value),

                                Freight =
                                    recordSet.Fields.Item("Freight")
                                        .Value?.ToString(),

                                UserSign =
                                    ToNullableInt(
                                        recordSet.Fields.Item("UserSign").Value),

                                ValidForAR =
                                    recordSet.Fields.Item("ValidForAR")
                                        .Value?.ToString(),

                                ValidForAP =
                                    recordSet.Fields.Item("ValidForAP")
                                        .Value?.ToString(),

                                TfcId =
                                    ToNullableInt(
                                        recordSet.Fields.Item("TfcId").Value),

                                Lock =
                                    recordSet.Fields.Item("Lock")
                                        .Value?.ToString(),

                                TaxIcms =
                                    recordSet.Fields.Item("TaxIcms")
                                        .Value?.ToString(),

                                IsItmLevel =
                                    recordSet.Fields.Item("IsItmLevel")
                                        .Value?.ToString(),

                                CfopIn =
                                    recordSet.Fields.Item("CfopIn")
                                        .Value?.ToString(),

                                CfopOut =
                                    recordSet.Fields.Item("CfopOut")
                                        .Value?.ToString(),

                                LogInstanc =
                                    ToNullableInt(
                                        recordSet.Fields.Item("LogInstanc").Value),

                                UserSign2 =
                                    ToNullableInt(
                                        recordSet.Fields.Item("UserSign2").Value),

                                UpdateDate =
                                    ToNullableDate(
                                        recordSet.Fields.Item("UpdateDate").Value),

                                FADebit =
                                    recordSet.Fields.Item("FADebit")
                                        .Value?.ToString()
                            });

                            recordSet.MoveNext();
                        }
                    }

                    return (IReadOnlyList<TaxResponse>)result;
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to retrieve tax codes: {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (recordSet is not null &&
                        Marshal.IsComObject(recordSet))
                    {
                        Marshal.FinalReleaseComObject(recordSet);
                    }
                }
            });
        }

        public Task<TaxResponse?> GetByCodeAsync(
            string code)
        {
            return StaTaskRunner.RunAsync<TaxResponse?>(() =>
            {
                using var handle =
                    _connectionService.Connect();

                dynamic? tax = null;

                try
                {
                    tax =
                        handle.GetBusinessObject(
                            SapObjectTypes.SalesTaxCodes,
                            "get tax code");

                    bool found =
                        tax.GetByKey(code);

                    if (!found)
                        return null;

                    return new TaxResponse
                    {
                        Code = tax.Code,
                        Name = tax.Name,
                        Rate = Convert.ToDecimal(tax.Rate),
                        Freight = tax.Freight?.ToString(),
                        ValidForAR = tax.ValidForAR?.ToString(),
                        ValidForAP = tax.ValidForAP?.ToString(),
                        Lock = tax.Lock?.ToString()
                    };
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to retrieve tax code '{code}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (tax is not null &&
                        Marshal.IsComObject(tax))
                    {
                        Marshal.FinalReleaseComObject(tax);
                    }
                }
            });
        }

        public Task<TaxResponse> CreateAsync(
            TaxRequest request)
        {
            return StaTaskRunner.RunAsync(() =>
            {
                using var handle =
                    _connectionService.Connect();

                dynamic? tax = null;

                try
                {
                    tax =
                        handle.GetBusinessObject(
                            SapObjectTypes.SalesTaxCodes,
                            "create tax code");

                    tax.Code = request.Code;
                    tax.Name = request.Name;
                    tax.Rate = request.Rate;

                    if (!string.IsNullOrWhiteSpace(request.Freight))
                        tax.Freight = request.Freight;

                    if (!string.IsNullOrWhiteSpace(request.ValidForAR))
                        tax.ValidForAR = request.ValidForAR;

                    if (!string.IsNullOrWhiteSpace(request.ValidForAP))
                        tax.ValidForAP = request.ValidForAP;

                    if (!string.IsNullOrWhiteSpace(request.Lock))
                        tax.Lock = request.Lock;

                    int result = tax.Add();

                    if (result != 0)
                    {
                        int errorCode =
                            handle.Company.GetLastErrorCode();

                        string errorMessage =
                            handle.Company.GetLastErrorDescription();

                        throw new SapException(
                            ErrorCodes.SapOperationFailed,
                            errorMessage,
                            errorCode);
                    }

                    _logger.LogInformation(
                        "SAP Tax Code {Code} created",
                        request.Code);

                    return new TaxResponse
                    {
                        Code = request.Code,
                        Name = request.Name,
                        Rate = request.Rate,
                        Freight = request.Freight,
                        ValidForAR = request.ValidForAR,
                        ValidForAP = request.ValidForAP,
                        Lock = request.Lock
                    };
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to create tax code '{request.Code}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (tax is not null &&
                        Marshal.IsComObject(tax))
                    {
                        Marshal.FinalReleaseComObject(tax);
                    }
                }
            });
        }

        public Task<TaxResponse?> UpdateAsync(
            string code,
            TaxUpdateRequest request)
        {
            return StaTaskRunner.RunAsync<TaxResponse?>(() =>
            {
                using var handle =
                    _connectionService.Connect();

                dynamic? tax = null;

                try
                {
                    tax =
                        handle.GetBusinessObject(
                            SapObjectTypes.SalesTaxCodes,
                            "update tax code");

                    bool found =
                        tax.GetByKey(code);

                    if (!found)
                        return null;

                    tax.Name = request.Name;
                    tax.Rate = request.Rate;

                    if (!string.IsNullOrWhiteSpace(request.Freight))
                        tax.Freight = request.Freight;

                    if (!string.IsNullOrWhiteSpace(request.ValidForAR))
                        tax.ValidForAR = request.ValidForAR;

                    if (!string.IsNullOrWhiteSpace(request.ValidForAP))
                        tax.ValidForAP = request.ValidForAP;

                    if (!string.IsNullOrWhiteSpace(request.Lock))
                        tax.Lock = request.Lock;

                    int result = tax.Update();

                    if (result != 0)
                    {
                        int errorCode =
                            handle.Company.GetLastErrorCode();

                        string errorMessage =
                            handle.Company.GetLastErrorDescription();

                        throw new SapException(
                            ErrorCodes.SapOperationFailed,
                            errorMessage,
                            errorCode);
                    }

                    _logger.LogInformation(
                        "SAP Tax Code {Code} updated",
                        code);

                    return new TaxResponse
                    {
                        Code = code,
                        Name = request.Name,
                        Rate = request.Rate,
                        Freight = request.Freight,
                        ValidForAR = request.ValidForAR,
                        ValidForAP = request.ValidForAP,
                        Lock = request.Lock
                    };
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to update tax code '{code}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (tax is not null &&
                        Marshal.IsComObject(tax))
                    {
                        Marshal.FinalReleaseComObject(tax);
                    }
                }
            });
        }

        public Task<bool> DeleteAsync(
            string code)
        {
            return StaTaskRunner.RunAsync<bool>(() =>
            {
                using var handle =
                    _connectionService.Connect();

                dynamic? tax = null;

                try
                {
                    tax =
                        handle.GetBusinessObject(
                            SapObjectTypes.SalesTaxCodes,
                            "delete tax code");

                    bool found =
                        tax.GetByKey(code);

                    if (!found)
                        return false;

                    int result = tax.Remove();

                    if (result != 0)
                    {
                        int errorCode =
                            handle.Company.GetLastErrorCode();

                        string errorMessage =
                            handle.Company.GetLastErrorDescription();

                        throw new SapException(
                            ErrorCodes.SapOperationFailed,
                            errorMessage,
                            errorCode);
                    }

                    _logger.LogInformation(
                        "SAP Tax Code {Code} deleted",
                        code);

                    return true;
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to delete tax code '{code}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (tax is not null &&
                        Marshal.IsComObject(tax))
                    {
                        Marshal.FinalReleaseComObject(tax);
                    }
                }
            });
        }

        private static string EscapeSql(string value)
        {
            return value.Replace("'", "''");
        }

        private static int? ToNullableInt(object? value)
        {
            if (value is null || value == DBNull.Value)
                return null;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return null;
        }

        private static DateTime? ToNullableDate(object? value)
        {
            if (value is null || value == DBNull.Value)
                return null;

            if (DateTime.TryParse(
                value.ToString(),
                out DateTime result))
            {
                return result;
            }

            return null;
        }
    }
}
