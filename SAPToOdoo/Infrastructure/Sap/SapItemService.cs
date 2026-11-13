using System.Runtime.InteropServices;
using SAPToOdoo.Application.DTOs.Items;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Infrastructure.Sap;

public sealed class SapItemService : ISapItemService
{
    private readonly ISapConnectionService _connectionService;
    private readonly ILogger<SapItemService> _logger;

    public SapItemService(ISapConnectionService connectionService, ILogger<SapItemService> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    public Task<ItemResponse?> GetByItemCodeAsync(string itemCode)
    {
        return StaTaskRunner.RunAsync<ItemResponse?>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? item = null;
            try
            {
                item = handle.GetBusinessObject(SapObjectTypes.Items, "look up the item");

                bool found = item.GetByKey(itemCode);
                if (!found)
                {
                    return null;
                }

                object rawCreateDate = item.CreateDate;

                return new ItemResponse
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemsGroupCode = item.ItemsGroupCode,
                    CreateDate = rawCreateDate is DateTime createDate ? createDate : null,
                    DefaultWarehouse = item.DefaultWarehouse,
                    UnitOfMeasure = item.InventoryUOM
                };
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve item '{itemCode}' from SAP: {ex.Message}", innerException: ex);
            }
            finally
            {
                if (item is not null && Marshal.IsComObject(item))
                {
                    Marshal.FinalReleaseComObject(item);
                }
            }
        });
    }

    public Task<ItemResponse> CreateAsync(ItemRequest request)
    {
        return StaTaskRunner.RunAsync(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? item = null;
            try
            {
                item = handle.GetBusinessObject(SapObjectTypes.Items, "create item");

                item.ItemCode = request.ItemCode;
                item.ItemName = request.ItemName;
                item.ItemsGroupCode = request.ItemsGroupCode!.Value;

                int result = item.Add();
                if (result != 0)
                {
                    int errorCode = handle.Company.GetLastErrorCode();
                    string errorMessage = handle.Company.GetLastErrorDescription();
                    throw new SapException(ErrorCodes.SapOperationFailed, errorMessage, errorCode);
                }

                string newItemCode = handle.Company.GetNewObjectKey();

                _logger.LogInformation("SAP Item {ItemCode} created", newItemCode);

                return new ItemResponse
                {
                    ItemCode = newItemCode,
                    ItemName = request.ItemName,
                    ItemsGroupCode = request.ItemsGroupCode!.Value
                };
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(ErrorCodes.SapOperationFailed,
                    $"Failed to create item '{request.ItemCode}' in SAP: {ex.Message}", innerException: ex);
            }
            finally
            {
                if (item is not null && Marshal.IsComObject(item))
                {
                    Marshal.FinalReleaseComObject(item);
                }
            }
        });
    }

    public Task<IReadOnlyList<ItemResponse>> GetLatestAsync(
    int limit,
    string? warehouseCode = null)
    {
        limit = Math.Clamp(limit, 1, 100);

        // Parse multiple comma-separated warehouse codes.
        var warehouseCodes = string.IsNullOrWhiteSpace(warehouseCode)
            ? new List<string>()
            : warehouseCode
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (warehouseCodes.Any(x => x.Length > 8))
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "Each warehouse code must be at most 8 characters.");
        }

        // Escape single quotes in warehouse codes.
        string warehouseInSql = string.Join(", ",
            warehouseCodes.Select(x => $"'{x.Replace("'", "''")}'"));

        bool hasWarehouseFilter = warehouseCodes.Count > 0;

        return StaTaskRunner.RunAsync<IReadOnlyList<ItemResponse>>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? recordSet = null;

            try
            {
                recordSet = handle.GetBusinessObjectFromCandidates(
                    SapObjectTypes.BoRecordsetCandidates,
                    "query latest items by warehouse");

                // When warehouse codes are supplied, select only items
                // that have a matching OITW record.
                string itemFilter = hasWarehouseFilter
                    ? $@"
WHERE EXISTS
(
    SELECT 1
    FROM OITW X
    WHERE X.ItemCode = OITM.ItemCode
      AND X.WhsCode IN ({warehouseInSql})
)"
                    : string.Empty;

                // INNER JOIN ensures each returned item has a warehouse record.
                // When a filter is supplied, include only those warehouses.
                string warehouseJoin = hasWarehouseFilter
                    ? $@"
INNER JOIN OITW W
    ON W.ItemCode = I.ItemCode
   AND W.WhsCode IN ({warehouseInSql})
INNER JOIN OWHS H
    ON H.WhsCode = W.WhsCode"
                    : @"
INNER JOIN OITW W
    ON W.ItemCode = I.ItemCode
INNER JOIN OWHS H
    ON H.WhsCode = W.WhsCode";

                string query = $@"
SELECT
    I.ItemCode,
    I.ItemName,
    I.ItmsGrpCod,
    I.CreateDate,
    I.DfltWH,
    I.InvntryUom,
    W.WhsCode,
    H.WhsName,
    W.OnHand
FROM
(
    SELECT TOP {limit}
        ItemCode,
        ItemName,
        ItmsGrpCod,
        CreateDate,
        DfltWH,
        InvntryUom
    FROM OITM
    {itemFilter}
    ORDER BY CreateDate DESC, ItemCode DESC
) I
{warehouseJoin}
ORDER BY
    I.CreateDate DESC,
    I.ItemCode DESC,
    W.WhsCode";

                recordSet.DoQuery(query);

                var items = new List<ItemResponse>();
                var byCode = new Dictionary<string, ItemResponse>(
                    StringComparer.OrdinalIgnoreCase);

                while (!(bool)recordSet.EoF)
                {
                    string itemCode =
                        Convert.ToString(
                            recordSet.Fields.Item("ItemCode").Value)
                        ?? string.Empty;

                    if (!byCode.TryGetValue(itemCode, out var item))
                    {
                        object rawCreateDate =
                            recordSet.Fields.Item("CreateDate").Value;

                        string? defaultWarehouse =
                            Convert.ToString(
                                recordSet.Fields.Item("DfltWH").Value);

                        string? uom =
                            Convert.ToString(
                                recordSet.Fields.Item("InvntryUom").Value);

                        item = new ItemResponse
                        {
                            ItemCode = itemCode,

                            ItemName =
                                Convert.ToString(
                                    recordSet.Fields.Item("ItemName").Value)
                                ?? string.Empty,

                            ItemsGroupCode = Convert.ToInt32(
                                recordSet.Fields.Item("ItmsGrpCod").Value),

                            CreateDate = rawCreateDate is DateTime createDate
                                ? createDate
                                : null,

                            DefaultWarehouse =
                                string.IsNullOrWhiteSpace(defaultWarehouse)
                                    ? null
                                    : defaultWarehouse,

                            UnitOfMeasure =
                                string.IsNullOrWhiteSpace(uom)
                                    ? null
                                    : uom
                        };

                        byCode[itemCode] = item;
                        items.Add(item);
                    }

                    string? resultWarehouseCode =
                        Convert.ToString(
                            recordSet.Fields.Item("WhsCode").Value);

                    if (!string.IsNullOrWhiteSpace(resultWarehouseCode))
                    {
                        object rawOnHand =
                            recordSet.Fields.Item("OnHand").Value;

                        item.Warehouses.Add(new WarehouseStockResponse
                        {
                            WarehouseCode = resultWarehouseCode,

                            WarehouseName =
                                Convert.ToString(
                                    recordSet.Fields.Item("WhsName").Value)
                                ?? string.Empty,

                            OnHand = rawOnHand is null || rawOnHand is DBNull
                                ? 0m
                                : Convert.ToDecimal(rawOnHand)
                        });
                    }

                    recordSet.MoveNext();
                }

                return (IReadOnlyList<ItemResponse>)items;
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve latest items from SAP: {ex.Message}",
                    innerException: ex);
            }
            finally
            {
                if (recordSet is not null && Marshal.IsComObject(recordSet))
                {
                    Marshal.FinalReleaseComObject(recordSet);
                }
            }
        });
    }

    public Task<ItemResponse?> UpdateAsync(string itemCode, ItemUpdateRequest request)
    {
        return StaTaskRunner.RunAsync<ItemResponse?>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? item = null;
            try
            {
                item = handle.GetBusinessObject(SapObjectTypes.Items, "look up the item");

                bool found = item.GetByKey(itemCode);
                if (!found)
                {
                    return null;
                }

                item.ItemName = request.ItemName;

                int result = item.Update();
                if (result != 0)
                {
                    int errorCode = handle.Company.GetLastErrorCode();
                    string errorMessage = handle.Company.GetLastErrorDescription();
                    throw new SapException(ErrorCodes.SapOperationFailed, errorMessage, errorCode);
                }

                _logger.LogInformation("SAP Item {ItemCode} updated", itemCode);

                return new ItemResponse
                {
                    ItemCode = itemCode,
                    ItemName = request.ItemName,
                    ItemsGroupCode = (int)item.ItemsGroupCode
                };
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(ErrorCodes.SapOperationFailed,
                    $"Failed to update item '{itemCode}' in SAP: {ex.Message}", innerException: ex);
            }
            finally
            {
                if (item is not null && Marshal.IsComObject(item))
                {
                    Marshal.FinalReleaseComObject(item);
                }
            }
        });
    }

    public Task<IReadOnlyList<ItemResponse>> GetByWarehouseAsync(IEnumerable<string> warehouseCodes, int limit = 20, int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        // Accept repeated parameters and comma-separated values, in any mix.
        var codes = (warehouseCodes ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "At least one warehouseCode is required.");
        }

        if (codes.Count > 50)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "At most 50 warehouse codes can be requested at once.");
        }

        if (codes.Any(x => x.Length > 8))
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "Each warehouse code must be at most 8 characters.");
        }

        string warehouseInSql = string.Join(", ",
            codes.Select(x => $"'{x.Replace("'", "''")}'"));

        return StaTaskRunner.RunAsync<IReadOnlyList<ItemResponse>>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? recordSet = null;

            try
            {
                recordSet = handle.GetBusinessObjectFromCandidates(
                    SapObjectTypes.BoRecordsetCandidates,
                    "query items by warehouse");

                // Step 1 (inner query): pick one page of items that have a stock
                //   record in the requested warehouse(s).
                // Step 2 (joins): attach only the requested warehouses to each item.
                string query = $@"
SELECT
    I.ItemCode,
    I.ItemName,
    I.ItmsGrpCod,
    I.CreateDate,
    I.DfltWH,
    I.InvntryUom,
    W.WhsCode,
    H.WhsName,
    W.OnHand
FROM
(
    SELECT
        O.ItemCode,
        O.ItemName,
        O.ItmsGrpCod,
        O.CreateDate,
        O.DfltWH,
        O.InvntryUom
    FROM OITM O WITH (NOLOCK)
    WHERE EXISTS
    (
        SELECT 1
        FROM OITW X WITH (NOLOCK)
        WHERE X.ItemCode = O.ItemCode
          AND X.WhsCode IN ({warehouseInSql})
    )
    ORDER BY O.CreateDate DESC, O.ItemCode DESC
    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY
) I
INNER JOIN OITW W WITH (NOLOCK)
    ON W.ItemCode = I.ItemCode
   AND W.WhsCode IN ({warehouseInSql})
INNER JOIN OWHS H WITH (NOLOCK)
    ON H.WhsCode = W.WhsCode
ORDER BY
    I.CreateDate DESC,
    I.ItemCode DESC,
    W.WhsCode";

                recordSet.DoQuery(query);

                var items = new List<ItemResponse>();
                var byCode = new Dictionary<string, ItemResponse>(StringComparer.OrdinalIgnoreCase);

                while (!(bool)recordSet.EoF)
                {
                    string itemCode =
                        Convert.ToString(recordSet.Fields.Item("ItemCode").Value) ?? string.Empty;

                    if (!byCode.TryGetValue(itemCode, out var item))
                    {
                        object rawCreateDate = recordSet.Fields.Item("CreateDate").Value;
                        string? dfltWh = Convert.ToString(recordSet.Fields.Item("DfltWH").Value);
                        string? uom = Convert.ToString(recordSet.Fields.Item("InvntryUom").Value);

                        item = new ItemResponse
                        {
                            ItemCode = itemCode,
                            ItemName = Convert.ToString(recordSet.Fields.Item("ItemName").Value) ?? string.Empty,
                            ItemsGroupCode = Convert.ToInt32(recordSet.Fields.Item("ItmsGrpCod").Value),
                            CreateDate = rawCreateDate is DateTime createDate ? createDate : null,
                            DefaultWarehouse = string.IsNullOrWhiteSpace(dfltWh) ? null : dfltWh,
                            UnitOfMeasure = string.IsNullOrWhiteSpace(uom) ? null : uom
                        };

                        byCode[itemCode] = item;
                        items.Add(item);
                    }

                    object rawOnHand = recordSet.Fields.Item("OnHand").Value;

                    item.Warehouses.Add(new WarehouseStockResponse
                    {
                        WarehouseCode = Convert.ToString(recordSet.Fields.Item("WhsCode").Value) ?? string.Empty,
                        WarehouseName = Convert.ToString(recordSet.Fields.Item("WhsName").Value) ?? string.Empty,
                        OnHand = rawOnHand is null || rawOnHand is DBNull ? 0m : Convert.ToDecimal(rawOnHand)
                    });

                    recordSet.MoveNext();
                }

                return (IReadOnlyList<ItemResponse>)items;
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve items by warehouse from SAP: {ex.Message}",
                    innerException: ex);
            }
            finally
            {
                if (recordSet is not null && Marshal.IsComObject(recordSet))
                {
                    Marshal.FinalReleaseComObject(recordSet);
                }
            }
        });
    }

    //    public Task<IReadOnlyList<RetailItemResponse>> GetRetailAsync(string? itemCode,
    //                                                                  IEnumerable<string>? warehouseCodes,
    //                                                                  int? priceList = null,
    //                                                                  int limit = 20,
    //                                                                  int offset = 0)
    //    {
    //        limit = Math.Clamp(limit, 1, 100);
    //        offset = Math.Max(offset, 0);

    //        if (priceList is < 1)
    //        {
    //            throw new SapException(
    //                ErrorCodes.SapOperationFailed,
    //                "priceList must be 1 or greater.");
    //        }

    //        // Accept repeated parameters and comma-separated values, in any mix.
    //        var codes = (warehouseCodes ?? Enumerable.Empty<string>())
    //            .Where(x => !string.IsNullOrWhiteSpace(x))
    //            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
    //            .Select(x => x.Trim())
    //            .Where(x => x.Length > 0)
    //            .Distinct(StringComparer.OrdinalIgnoreCase)
    //            .ToList();

    //        if (codes.Count > 50)
    //        {
    //            throw new SapException(
    //                ErrorCodes.SapOperationFailed,
    //                "At most 50 warehouse codes can be requested at once.");
    //        }

    //        if (codes.Any(x => x.Length > 8))
    //        {
    //            throw new SapException(
    //                ErrorCodes.SapOperationFailed,
    //                "Each warehouse code must be at most 8 characters.");
    //        }

    //        // DoQuery has no parameters, so escape single quotes.
    //        string? item = string.IsNullOrWhiteSpace(itemCode)
    //            ? null
    //            : itemCode.Trim().Replace("'", "''");

    //        string whsIn = string.Join(", ",
    //            codes.Select(x => $"'{x.Replace("'", "''")}'"));

    //        string itemFilter = item is null ? "" : $"AND T0.ItemCode = '{item}'";
    //        string whsFilterX = codes.Count == 0 ? "" : $"AND X.WhsCode IN ({whsIn})";
    //        string whsFilterW = codes.Count == 0 ? "" : $"AND W.WhsCode IN ({whsIn})";

    //        // Price column and join only when a price list is requested.
    //        string priceSelect = priceList.HasValue
    //            ? "P.Price"
    //            : "CAST(NULL AS decimal(19,6)) AS Price";

    //        string priceJoin = priceList.HasValue
    //            ? $"LEFT JOIN ITM1 P WITH (NOLOCK) ON P.ItemCode = I.ItemCode AND P.PriceList = {priceList.Value}"
    //            : "";

    //        return StaTaskRunner.RunAsync<IReadOnlyList<RetailItemResponse>>(() =>
    //        {
    //            using var handle = _connectionService.Connect();
    //            dynamic? recordSet = null;

    //            try
    //            {
    //                recordSet = handle.GetBusinessObjectFromCandidates(
    //                    SapObjectTypes.BoRecordsetCandidates,
    //                    "query retail items");

    //                string query = $@"
    //SELECT
    //    I.ItemCode,
    //    I.ItemName,
    //    I.VatGourpSa,
    //    B.ItmsGrpNam,
    //    I.CodeBars,
    //    I.U_HSCode,
    //    {priceSelect},
    //    W.WhsCode,
    //    H.WhsName,
    //    W.OnHand
    //FROM
    //(
    //    SELECT T0.ItemCode, T0.ItemName, T0.VatGourpSa, T0.CodeBars,
    //           T0.U_HSCode, T0.ItmsGrpCod
    //    FROM OITM T0 WITH (NOLOCK)
    //    WHERE T0.U_ProdType = 'Retail'
    //      {itemFilter}
    //      AND EXISTS
    //      (
    //          SELECT 1 FROM OITW X WITH (NOLOCK)
    //          WHERE X.ItemCode = T0.ItemCode
    //            AND X.OnHand > 0
    //            {whsFilterX}
    //      )
    //    ORDER BY T0.ItemCode
    //    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY
    //) I
    //INNER JOIN OITB B WITH (NOLOCK) ON B.ItmsGrpCod = I.ItmsGrpCod
    //INNER JOIN OITW W WITH (NOLOCK) ON W.ItemCode = I.ItemCode AND W.OnHand > 0 {whsFilterW}
    //LEFT JOIN OWHS H WITH (NOLOCK) ON H.WhsCode = W.WhsCode
    //{priceJoin}
    //ORDER BY I.ItemCode, W.WhsCode";

    //                recordSet.DoQuery(query);

    //                var items = new List<RetailItemResponse>();
    //                var byCode = new Dictionary<string, RetailItemResponse>(
    //                    StringComparer.OrdinalIgnoreCase);

    //                while (!(bool)recordSet.EoF)
    //                {
    //                    string code =
    //                        Convert.ToString(recordSet.Fields.Item("ItemCode").Value)
    //                        ?? string.Empty;

    //                    if (!byCode.TryGetValue(code, out var row))
    //                    {
    //                        object rawPrice = recordSet.Fields.Item("Price").Value;

    //                        row = new RetailItemResponse
    //                        {
    //                            ItemCode = code,
    //                            ItemName =
    //                                Convert.ToString(recordSet.Fields.Item("ItemName").Value)
    //                                ?? string.Empty,
    //                            VatGroup = NullIfEmpty(recordSet.Fields.Item("VatGourpSa").Value),
    //                            ItemGroupName = NullIfEmpty(recordSet.Fields.Item("ItmsGrpNam").Value),
    //                            Barcode = NullIfEmpty(recordSet.Fields.Item("CodeBars").Value),
    //                            HsCode = NullIfEmpty(recordSet.Fields.Item("U_HSCode").Value),
    //                            PriceList = priceList,
    //                            Price = rawPrice is null || rawPrice is DBNull
    //                                ? null
    //                                : Convert.ToDecimal(rawPrice)
    //                        };

    //                        byCode[code] = row;
    //                        items.Add(row);
    //                    }

    //                    object rawOnHand = recordSet.Fields.Item("OnHand").Value;

    //                    row.Warehouses.Add(new WarehouseStockResponse
    //                    {
    //                        WarehouseCode =
    //                            Convert.ToString(recordSet.Fields.Item("WhsCode").Value)
    //                            ?? string.Empty,
    //                        WarehouseName =
    //                            Convert.ToString(recordSet.Fields.Item("WhsName").Value)
    //                            ?? string.Empty,
    //                        OnHand = rawOnHand is null || rawOnHand is DBNull
    //                            ? 0m
    //                            : Convert.ToDecimal(rawOnHand)
    //                    });

    //                    recordSet.MoveNext();
    //                }

    //                return (IReadOnlyList<RetailItemResponse>)items;
    //            }
    //            catch (SapException)
    //            {
    //                throw;
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new SapException(
    //                    ErrorCodes.SapOperationFailed,
    //                    $"Failed to retrieve retail items from SAP: {ex.Message}",
    //                    innerException: ex);
    //            }
    //            finally
    //            {
    //                if (recordSet is not null && Marshal.IsComObject(recordSet))
    //                {
    //                    Marshal.FinalReleaseComObject(recordSet);
    //                }
    //            }
    //        });


    public Task<IReadOnlyList<RetailItemResponse>> GetRetailAsync(
    string? itemCode,
    IEnumerable<string>? warehouseCodes,
    int? priceList = null,
    int limit = 20,
    int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        if (priceList is < 1)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "priceList must be 1 or greater.");
        }

        var codes = (warehouseCodes ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count > 50)
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "At most 50 warehouse codes can be requested at once.");
        }

        if (codes.Any(x => x.Length > 8))
        {
            throw new SapException(
                ErrorCodes.SapOperationFailed,
                "Each warehouse code must be at most 8 characters.");
        }

        string? item = string.IsNullOrWhiteSpace(itemCode)
            ? null
            : itemCode.Trim().Replace("'", "''");

        string whsIn = string.Join(", ",
            codes.Select(x => $"'{x.Replace("'", "''")}'"));

        string itemFilter = item is null ? "" : $"AND T0.ItemCode = '{item}'";
        string whsFilterX = codes.Count == 0 ? "" : $"AND X.WhsCode IN ({whsIn})";
        string whsFilterW = codes.Count == 0 ? "" : $"AND W.WhsCode IN ({whsIn})";

        // Only one specific list when requested; otherwise every list.
        string priceFilter = priceList.HasValue
            ? $"AND P.PriceList = {priceList.Value}"
            : "";

        // The page of items. Used by BOTH queries so they always agree.
        string pageSql = $@"
    SELECT T0.ItemCode, T0.ItemName, T0.VatGourpSa, T0.CodeBars,
           T0.U_HSCode, T0.ItmsGrpCod
    FROM OITM T0 WITH (NOLOCK)
    WHERE T0.U_ProdType = 'Retail'
      {itemFilter}
      AND EXISTS
      (
          SELECT 1 FROM OITW X WITH (NOLOCK)
          WHERE X.ItemCode = T0.ItemCode
            AND X.OnHand > 0
            {whsFilterX}
      )
    ORDER BY T0.ItemCode
    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

        return StaTaskRunner.RunAsync<IReadOnlyList<RetailItemResponse>>(() =>
        {
            using var handle = _connectionService.Connect();
            dynamic? recordSet = null;

            try
            {
                recordSet = handle.GetBusinessObjectFromCandidates(
                    SapObjectTypes.BoRecordsetCandidates,
                    "query retail items");

                // ---- Query 1: items and warehouses with stock ----
                string itemsQuery = $@"
SELECT
    I.ItemCode,
    I.ItemName,
    I.VatGourpSa,
    B.ItmsGrpNam,
    I.CodeBars,
    I.U_HSCode,
    W.WhsCode,
    H.WhsName,
    W.OnHand
FROM
(
{pageSql}
) I
INNER JOIN OITB B WITH (NOLOCK) ON B.ItmsGrpCod = I.ItmsGrpCod
INNER JOIN OITW W WITH (NOLOCK) ON W.ItemCode = I.ItemCode AND W.OnHand > 0 {whsFilterW}
LEFT JOIN OWHS H WITH (NOLOCK) ON H.WhsCode = W.WhsCode
ORDER BY I.ItemCode, W.WhsCode";

                recordSet.DoQuery(itemsQuery);

                var items = new List<RetailItemResponse>();
                var byCode = new Dictionary<string, RetailItemResponse>(
                    StringComparer.OrdinalIgnoreCase);

                while (!(bool)recordSet.EoF)
                {
                    string code =
                        Convert.ToString(recordSet.Fields.Item("ItemCode").Value)
                        ?? string.Empty;

                    if (!byCode.TryGetValue(code, out var row))
                    {
                        row = new RetailItemResponse
                        {
                            ItemCode = code,
                            ItemName =
                                Convert.ToString(recordSet.Fields.Item("ItemName").Value)
                                ?? string.Empty,
                            VatGroup = NullIfEmpty(recordSet.Fields.Item("VatGourpSa").Value),
                            ItemGroupName = NullIfEmpty(recordSet.Fields.Item("ItmsGrpNam").Value),
                            Barcode = NullIfEmpty(recordSet.Fields.Item("CodeBars").Value),
                            HsCode = NullIfEmpty(recordSet.Fields.Item("U_HSCode").Value)
                        };

                        byCode[code] = row;
                        items.Add(row);
                    }

                    object rawOnHand = recordSet.Fields.Item("OnHand").Value;

                    row.Warehouses.Add(new WarehouseStockResponse
                    {
                        WarehouseCode =
                            Convert.ToString(recordSet.Fields.Item("WhsCode").Value)
                            ?? string.Empty,
                        WarehouseName =
                            Convert.ToString(recordSet.Fields.Item("WhsName").Value)
                            ?? string.Empty,
                        OnHand = rawOnHand is null || rawOnHand is DBNull
                            ? 0m
                            : Convert.ToDecimal(rawOnHand)
                    });

                    recordSet.MoveNext();
                }

                // ---- Query 2: prices for the same page of items ----
                if (items.Count > 0)
                {
                    string pricesQuery = $@"
SELECT
    I.ItemCode,
    P.PriceList,
    L.ListName,
    P.Price
FROM
(
{pageSql}
) I
INNER JOIN ITM1 P WITH (NOLOCK)
    ON P.ItemCode = I.ItemCode
   AND P.Price IS NOT NULL
   {priceFilter}
LEFT JOIN OPLN L WITH (NOLOCK) ON L.ListNum = P.PriceList
ORDER BY I.ItemCode, P.PriceList";

                    recordSet.DoQuery(pricesQuery);

                    while (!(bool)recordSet.EoF)
                    {
                        string code =
                            Convert.ToString(recordSet.Fields.Item("ItemCode").Value)
                            ?? string.Empty;

                        if (byCode.TryGetValue(code, out var target))
                        {
                            object rawPrice = recordSet.Fields.Item("Price").Value;

                            target.Prices.Add(new PriceResponse
                            {
                                PriceList = Convert.ToInt32(
                                    recordSet.Fields.Item("PriceList").Value),
                                PriceListName = NullIfEmpty(
                                    recordSet.Fields.Item("ListName").Value),
                                Price = rawPrice is null || rawPrice is DBNull
                                    ? 0m
                                    : Convert.ToDecimal(rawPrice)
                            });
                        }

                        recordSet.MoveNext();
                    }
                }

                return (IReadOnlyList<RetailItemResponse>)items;
            }
            catch (SapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SapException(
                    ErrorCodes.SapOperationFailed,
                    $"Failed to retrieve retail items from SAP: {ex.Message}",
                    innerException: ex);
            }
            finally
            {
                if (recordSet is not null && Marshal.IsComObject(recordSet))
                {
                    Marshal.FinalReleaseComObject(recordSet);
                }
            }
        });

        static string? NullIfEmpty(object? v)
        {
            var s = Convert.ToString(v);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }

    static string? NullIfEmpty(object? v)
    {
        var s = Convert.ToString(v);
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
