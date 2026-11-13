using SAPToOdoo.Application.DTOs.Warehouse;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using System.Runtime.InteropServices;

namespace SAPToOdoo.Infrastructure.Sap
{
    public sealed class SapWarehouseService : ISapWarehouseService
    {
        private readonly ISapConnectionService _connectionService;
        private readonly ILogger<SapWarehouseService> _logger;

        public SapWarehouseService(
            ISapConnectionService connectionService,
            ILogger<SapWarehouseService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        public Task<IReadOnlyList<WarehouseResponse>> GetAsync(
            string? search,
            string? whsCode,
            string? whsName,
            string? locked,
            int limit)
        {
            return StaTaskRunner.RunAsync<IReadOnlyList<WarehouseResponse>>(() =>
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
                            "get warehouses");

                    var where = new List<string>();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        string value = EscapeSql(search);

                        where.Add($@"
                        (
                            WhsCode LIKE '%{value}%'
                            OR WhsName LIKE '%{value}%'
                        )");
                    }

                    if (!string.IsNullOrWhiteSpace(whsCode))
                    {
                        string value = EscapeSql(whsCode);

                        where.Add(
                            $"WhsCode = '{value}'");
                    }

                    if (!string.IsNullOrWhiteSpace(whsName))
                    {
                        string value = EscapeSql(whsName);

                        where.Add(
                            $"WhsName LIKE '%{value}%'");
                    }

                    if (!string.IsNullOrWhiteSpace(locked))
                    {
                        string value = EscapeSql(locked);

                        where.Add(
                            $"Locked = '{value}'");
                    }

                    string whereSql = where.Count > 0
                        ? "WHERE " + string.Join(" AND ", where)
                        : string.Empty;

                    string query = $@"
                    SELECT TOP {limit}
                        WhsCode,
                        WhsName,
                        IntrnalKey,
                        Grp_Code,
                        BalInvntAc,
                        SaleCostAc,
                        TransferAc,
                        Locked,
                        DataSource
                    FROM OWHS
                    {whereSql}
                    ORDER BY WhsCode";

                    recordSet.DoQuery(query);

                    var result =
                        new List<WarehouseResponse>();

                    if (!(bool)recordSet.EoF)
                    {
                        recordSet.MoveFirst();

                        while (!(bool)recordSet.EoF)
                        {
                            result.Add(new WarehouseResponse
                            {
                                WhsCode =
                                    recordSet.Fields.Item("WhsCode").Value?.ToString()
                                    ?? string.Empty,

                                WhsName =
                                    recordSet.Fields.Item("WhsName").Value?.ToString()
                                    ?? string.Empty,

                                IntrnalKey =
                                    recordSet.Fields.Item("IntrnalKey").Value?.ToString(),

                                GrpCode =
                                    recordSet.Fields.Item("Grp_Code").Value?.ToString(),

                                InventoryAccount =
                                    recordSet.Fields.Item("BalInvntAc").Value?.ToString(),

                                CostOfGoodsSoldAccount =
                                    recordSet.Fields.Item("SaleCostAc").Value?.ToString(),

                                TransferAccount =
                                    recordSet.Fields.Item("TransferAc").Value?.ToString(),

                                Locked =
                                    recordSet.Fields.Item("Locked").Value?.ToString(),

                                DataSource =
                                    recordSet.Fields.Item("DataSource").Value?.ToString()
                            });

                            recordSet.MoveNext();
                        }
                    }

                    return (IReadOnlyList<WarehouseResponse>)result;
                }
                catch (SapException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SapException(
                        ErrorCodes.SapOperationFailed,
                        $"Failed to retrieve warehouses: {ex.Message}",
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

        public Task<WarehouseResponse?> GetByCodeAsync(
            string whsCode)
        {
            return StaTaskRunner.RunAsync<WarehouseResponse?>(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? warehouse = null;

                try
                {
                    warehouse =
                        handle.GetBusinessObject(
                            SapObjectTypes.Warehouses,
                            "get warehouse");

                    bool found = warehouse.GetByKey(whsCode);

                    if (!found)
                        return null;

                    return new WarehouseResponse
                    {
                        WhsCode = warehouse.WarehouseCode,
                        WhsName = warehouse.WarehouseName,
                        IntrnalKey = warehouse.IntrnalKey?.ToString(),
                        GrpCode = warehouse.GroupCode?.ToString(),
                        InventoryAccount = warehouse.InventoryAccount,
                        CostOfGoodsSoldAccount = warehouse.CostAccount,
                        TransferAccount = warehouse.TransferAccount,
                        Locked = warehouse.Locked?.ToString(),
                        DataSource = warehouse.DataSource?.ToString()
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
                        $"Failed to retrieve warehouse '{whsCode}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (warehouse is not null &&
                        Marshal.IsComObject(warehouse))
                    {
                        Marshal.FinalReleaseComObject(warehouse);
                    }
                }
            });
        }

        public Task<WarehouseResponse> CreateAsync(
            WarehouseRequest request)
        {
            return StaTaskRunner.RunAsync(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? warehouse = null;

                try
                {
                    warehouse =
                        handle.GetBusinessObject(
                            SapObjectTypes.Warehouses,
                            "create warehouse");

                    warehouse.WarehouseCode =
                        request.WhsCode;

                    warehouse.WarehouseName =
                        request.WhsName;

                    if (!string.IsNullOrWhiteSpace(request.GrpCode))
                        warehouse.GroupCode =
                            request.GrpCode;

                    if (!string.IsNullOrWhiteSpace(request.InventoryAccount))
                        warehouse.InventoryAccount =
                            request.InventoryAccount;

                    if (!string.IsNullOrWhiteSpace(request.CostOfGoodsSoldAccount))
                        warehouse.CostAccount =
                            request.CostOfGoodsSoldAccount;

                    if (!string.IsNullOrWhiteSpace(request.TransferAccount))
                        warehouse.TransferAccount =
                            request.TransferAccount;

                    int result = warehouse.Add();

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

                    string newCode =
                        handle.Company.GetNewObjectKey();

                    _logger.LogInformation(
                        "SAP Warehouse {WarehouseCode} created",
                        newCode);

                    return new WarehouseResponse
                    {
                        WhsCode = newCode,
                        WhsName = request.WhsName,
                        GrpCode = request.GrpCode,
                        InventoryAccount = request.InventoryAccount,
                        CostOfGoodsSoldAccount =
                            request.CostOfGoodsSoldAccount,
                        TransferAccount =
                            request.TransferAccount
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
                        $"Failed to create warehouse '{request.WhsCode}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (warehouse is not null &&
                        Marshal.IsComObject(warehouse))
                    {
                        Marshal.FinalReleaseComObject(warehouse);
                    }
                }
            });
        }

        public Task<WarehouseResponse?> UpdateAsync(
            string whsCode,
            WarehouseUpdateRequest request)
        {
            return StaTaskRunner.RunAsync<WarehouseResponse?>(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? warehouse = null;

                try
                {
                    warehouse =
                        handle.GetBusinessObject(
                            SapObjectTypes.Warehouses,
                            "update warehouse");

                    bool found =
                        warehouse.GetByKey(whsCode);

                    if (!found)
                        return null;

                    warehouse.WarehouseName =
                        request.WhsName;

                    if (!string.IsNullOrWhiteSpace(request.GrpCode))
                        warehouse.GroupCode =
                            request.GrpCode;

                    if (!string.IsNullOrWhiteSpace(request.InventoryAccount))
                        warehouse.InventoryAccount =
                            request.InventoryAccount;

                    if (!string.IsNullOrWhiteSpace(request.CostOfGoodsSoldAccount))
                        warehouse.CostAccount =
                            request.CostOfGoodsSoldAccount;

                    if (!string.IsNullOrWhiteSpace(request.TransferAccount))
                        warehouse.TransferAccount =
                            request.TransferAccount;

                    if (!string.IsNullOrWhiteSpace(request.Locked))
                        warehouse.Locked =
                            request.Locked;

                    int result = warehouse.Update();

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
                        "SAP Warehouse {WarehouseCode} updated",
                        whsCode);

                    return new WarehouseResponse
                    {
                        WhsCode = whsCode,
                        WhsName = request.WhsName,
                        GrpCode = request.GrpCode,
                        InventoryAccount = request.InventoryAccount,
                        CostOfGoodsSoldAccount =
                            request.CostOfGoodsSoldAccount,
                        TransferAccount =
                            request.TransferAccount,
                        Locked = request.Locked
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
                        $"Failed to update warehouse '{whsCode}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (warehouse is not null &&
                        Marshal.IsComObject(warehouse))
                    {
                        Marshal.FinalReleaseComObject(warehouse);
                    }
                }
            });
        }

        public Task<bool> DeleteAsync(string whsCode)
        {
            return StaTaskRunner.RunAsync<bool>(() =>
            {
                using var handle = _connectionService.Connect();

                dynamic? warehouse = null;

                try
                {
                    warehouse =
                        handle.GetBusinessObject(
                            SapObjectTypes.Warehouses,
                            "delete warehouse");

                    bool found =
                        warehouse.GetByKey(whsCode);

                    if (!found)
                        return false;

                    int result = warehouse.Remove();

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
                        "SAP Warehouse {WarehouseCode} deleted",
                        whsCode);

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
                        $"Failed to delete warehouse '{whsCode}': {ex.Message}",
                        innerException: ex);
                }
                finally
                {
                    if (warehouse is not null &&
                        Marshal.IsComObject(warehouse))
                    {
                        Marshal.FinalReleaseComObject(warehouse);
                    }
                }
            });
        }

        private static string EscapeSql(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
