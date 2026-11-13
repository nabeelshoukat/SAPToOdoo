using System.Runtime.InteropServices;
using SAPToOdoo.Application.DTOs.BusinessPartners;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Infrastructure.Sap;

public sealed class SapBusinessPartnerService : ISapBusinessPartnerService
{
    private readonly ISapConnectionService _connectionService;
    private readonly ILogger<SapBusinessPartnerService> _logger;

    public SapBusinessPartnerService(ISapConnectionService connectionService, ILogger<SapBusinessPartnerService> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    public Task<BusinessPartnerResponse?> GetByCardCodeAsync(string cardCode)
    {
        return StaTaskRunner.RunAsync<BusinessPartnerResponse?>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? businessPartner = null;
            try
            {
                businessPartner = handle.GetBusinessObject(SapObjectTypes.BusinessPartners, "look up the business partner");

                bool found = businessPartner.GetByKey(cardCode);
                if (!found)
                {
                    return null;
                }

                return new BusinessPartnerResponse
                {
                    CardCode = businessPartner.CardCode,
                    CardName = businessPartner.CardName,
                    Phone = businessPartner.Phone1,
                    Email = businessPartner.EmailAddress
                };
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve Business Partner '{cardCode}' from SAP: {ex.Message}", innerException: ex);
            }
            finally
            {
                if (businessPartner is not null && Marshal.IsComObject(businessPartner))
                {
                    Marshal.FinalReleaseComObject(businessPartner);
                }
            }
        });
    }

    public Task<BusinessPartnerResponse?> UpdateAsync(string cardCode, BusinessPartnerUpdateRequest request)
    {
        return StaTaskRunner.RunAsync<BusinessPartnerResponse?>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? businessPartner = null;
            try
            {
                businessPartner = handle.GetBusinessObject(SapObjectTypes.BusinessPartners, "look up the business partner");

                bool found = businessPartner.GetByKey(cardCode);
                if (!found)
                {
                    return null;
                }

                businessPartner.CardName = request.CardName;

                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    businessPartner.Phone1 = request.Phone;
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    businessPartner.EmailAddress = request.Email;
                }

                int result = businessPartner.Update();
                if (result != 0)
                {
                    int errorCode = handle.Company.GetLastErrorCode();
                    string errorMessage = handle.Company.GetLastErrorDescription();
                    throw new SapException(ErrorCodes.SapOperationFailed, errorMessage, errorCode);
                }

                _logger.LogInformation("SAP Business Partner {CardCode} updated", cardCode);

                return new BusinessPartnerResponse
                {
                    CardCode = cardCode,
                    CardName = request.CardName,
                    Phone = request.Phone,
                    Email = request.Email
                };
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(ErrorCodes.SapOperationFailed,
                    $"Failed to update Business Partner '{cardCode}' in SAP: {ex.Message}", innerException: ex);
            }
            finally
            {
                if (businessPartner is not null && Marshal.IsComObject(businessPartner))
                {
                    Marshal.FinalReleaseComObject(businessPartner);
                }
            }
        });
    }

    public Task<BusinessPartnerResponse> CreateAsync(BusinessPartnerRequest request)
    {
        return StaTaskRunner.RunAsync(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? businessPartner = null;

            try
            {
                businessPartner = handle.GetBusinessObject(
                    SapObjectTypes.BusinessPartners,
                    "create business partner");

                businessPartner.CardCode = request.CardCode;
                businessPartner.CardName = request.CardName;
                businessPartner.CardType = 0;

                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    businessPartner.Phone1 = request.Phone;
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    businessPartner.EmailAddress = request.Email;
                }

                int result = businessPartner.Add();

                if (result != 0)
                {
                    int errorCode = handle.Company.GetLastErrorCode();
                    string errorMessage = handle.Company.GetLastErrorDescription();

                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        errorMessage,
                        errorCode);
                }

                string newCardCode = handle.Company.GetNewObjectKey();

                _logger.LogInformation(
                    "SAP Business Partner {CardCode} created",
                    newCardCode);

                return new BusinessPartnerResponse
                {
                    CardCode = newCardCode,
                    CardName = request.CardName,
                    Phone = request.Phone,
                    Email = request.Email
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
                    $"Failed to create Business Partner '{request.CardCode}' in SAP: {ex.Message}",
                    innerException: ex);
            }
            finally
            {
                if (businessPartner is not null &&
                    Marshal.IsComObject(businessPartner))
                {
                    Marshal.FinalReleaseComObject(businessPartner);
                }
            }
        });
    }

    public Task<IReadOnlyList<BusinessPartnerListResponse>> GetAsync(
    string? search,
    string? cardCode,
    string? cardName,
    string? cardType,
    int? limit)
    {
        return StaTaskRunner.RunAsync<IReadOnlyList<BusinessPartnerListResponse>>(() =>
        {
            using var handle = _connectionService.Connect();

            dynamic? recordSet = null;

            try
            {
                // Default limit = 20
                limit ??= 20;

                // Maximum limit = 100
                if (limit > 100)
                    limit = 100;

                recordSet =
                    handle.GetBusinessObjectFromCandidates(
                        SapObjectTypes.BoRecordsetCandidates,
                        "get business partners");

                var where = new List<string>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string value = EscapeSql(search);

                    where.Add($@"
                (
                    CardCode LIKE '%{value}%'
                    OR CardName LIKE '%{value}%'
                    OR Phone1 LIKE '%{value}%'
                    OR E_Mail LIKE '%{value}%'
                )");
                }

                if (!string.IsNullOrWhiteSpace(cardCode))
                {
                    string value = EscapeSql(cardCode);

                    where.Add($"CardCode = '{value}'");
                }

                if (!string.IsNullOrWhiteSpace(cardName))
                {
                    string value = EscapeSql(cardName);

                    where.Add($"CardName LIKE '%{value}%'");
                }

                if (!string.IsNullOrWhiteSpace(cardType))
                {
                    string value = EscapeSql(cardType);

                    where.Add($"CardType = '{value}'");
                }

                string whereSql = where.Count > 0
                    ? "WHERE " + string.Join(" AND ", where)
                    : string.Empty;

                string query = $@"
            SELECT TOP {limit}
                CardCode,
                CardName,
                CardType,
                CmpPrivate,
                GroupCode,
                CntctPrsn,
                BillToDef,
                Address,
                City,
                ZipCode,
                Country,
                MailCountr,
                Phone1,
                Cellular,
                Phone2,
                E_Mail,
                IntrntSite,
                Notes,
                Free_Text,
                VatIdUnCmp,
                NINum,
                LicTradNum,
                Balance,
                BalanceSys,
                CreditLine,
                DebtLine,
                DebPayAcct,
                HouseBank,
                HousBnkAct,
                HousBnkCry,
                validFor,
                frozenFor,
                ShipToDef,
                CreateDate,
                UpdateDate
            FROM OCRD
            {whereSql}
            ORDER BY CardCode";

                recordSet.DoQuery(query);

                var result =
                    new List<BusinessPartnerListResponse>();

                if (!(bool)recordSet.EoF)
                {
                    recordSet.MoveFirst();

                    while (!(bool)recordSet.EoF)
                    {
                        result.Add(new BusinessPartnerListResponse
                        {
                            CardCode =
                                recordSet.Fields.Item("CardCode").Value?.ToString(),

                            CardName =
                                recordSet.Fields.Item("CardName").Value?.ToString(),

                            CardType =
                                recordSet.Fields.Item("CardType").Value?.ToString(),

                            CmpPrivate =
                                recordSet.Fields.Item("CmpPrivate").Value?.ToString(),

                            GroupCode =
                                recordSet.Fields.Item("GroupCode").Value?.ToString(),

                            CntctPrsn =
                                recordSet.Fields.Item("CntctPrsn").Value?.ToString(),

                            BillToDef =
                                recordSet.Fields.Item("BillToDef").Value?.ToString(),

                            Address =
                                recordSet.Fields.Item("Address").Value?.ToString(),

                            City =
                                recordSet.Fields.Item("City").Value?.ToString(),

                            ZipCode =
                                recordSet.Fields.Item("ZipCode").Value?.ToString(),

                            Country =
                                recordSet.Fields.Item("Country").Value?.ToString(),

                            MailCountr =
                                recordSet.Fields.Item("MailCountr").Value?.ToString(),

                            Phone1 =
                                recordSet.Fields.Item("Phone1").Value?.ToString(),

                            Cellular =
                                recordSet.Fields.Item("Cellular").Value?.ToString(),

                            Phone2 =
                                recordSet.Fields.Item("Phone2").Value?.ToString(),

                            E_Mail =
                                recordSet.Fields.Item("E_Mail").Value?.ToString(),

                            IntrntSite =
                                recordSet.Fields.Item("IntrntSite").Value?.ToString(),

                            Notes =
                                recordSet.Fields.Item("Notes").Value?.ToString(),

                            Free_Text =
                                recordSet.Fields.Item("Free_Text").Value?.ToString(),

                            VatIdUnCmp =
                                recordSet.Fields.Item("VatIdUnCmp").Value?.ToString(),

                            NINum =
                                recordSet.Fields.Item("NINum").Value?.ToString(),

                            LicTradNum =
                                recordSet.Fields.Item("LicTradNum").Value?.ToString(),

                            Balance =
                                GetNullableDecimal(recordSet, "Balance"),

                            BalanceSys =
                                GetNullableDecimal(recordSet, "BalanceSys"),

                            CreditLine =
                                GetNullableDecimal(recordSet, "CreditLine"),

                            DebtLine =
                                GetNullableDecimal(recordSet, "DebtLine"),

                            DebPayAcct =
                                recordSet.Fields.Item("DebPayAcct").Value?.ToString(),

                            HouseBank =
                                recordSet.Fields.Item("HouseBank").Value?.ToString(),

                            HousBnkAct =
                                recordSet.Fields.Item("HousBnkAct").Value?.ToString(),

                            HousBnkCry =
                                recordSet.Fields.Item("HousBnkCry").Value?.ToString(),

                            ValidFor =
                                recordSet.Fields.Item("validFor").Value?.ToString(),

                            FrozenFor =
                                recordSet.Fields.Item("frozenFor").Value?.ToString(),

                            ShipToDef =
                                recordSet.Fields.Item("ShipToDef").Value?.ToString(),

                            CreateDate =
                                GetNullableDateTime(recordSet, "CreateDate"),

                            UpdateDate =
                                GetNullableDateTime(recordSet, "UpdateDate")
                        });

                        recordSet.MoveNext();
                    }
                }

                return (IReadOnlyList<BusinessPartnerListResponse>)result;
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve business partners: {ex.Message}",
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

    private static decimal? GetNullableDecimal(
    dynamic recordSet,
    string fieldName)
    {
        object value = recordSet.Fields.Item(fieldName).Value;

        if (value is null || Convert.IsDBNull(value))
            return null;

        return Convert.ToDecimal(value);
    }

    private static DateTime? GetNullableDateTime(
        dynamic recordSet,
        string fieldName)
    {
        object value = recordSet.Fields.Item(fieldName).Value;

        if (value is null || Convert.IsDBNull(value))
            return null;

        return Convert.ToDateTime(value);
    }
    private static string EscapeSql(string value)
    {
        return value.Replace("'", "''");
    }
}
