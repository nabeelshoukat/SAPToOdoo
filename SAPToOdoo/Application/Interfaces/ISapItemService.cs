using SAPToOdoo.Application.DTOs.Items;

namespace SAPToOdoo.Application.Interfaces;

public interface ISapItemService
{
    Task<ItemResponse?> GetByItemCodeAsync(string itemCode);
    Task<ItemResponse> CreateAsync(ItemRequest request);
    Task<ItemResponse?> UpdateAsync(string itemCode, ItemUpdateRequest request);
    Task<IReadOnlyList<ItemResponse>> GetLatestAsync(int limit, string? warehouseCode = null);
    Task<IReadOnlyList<ItemResponse>> GetByWarehouseAsync(IEnumerable<string> warehouseCodes, int limit = 20, int offset = 0);
    Task<IReadOnlyList<RetailItemResponse>> GetRetailAsync(string? itemCode, IEnumerable<string>? warehouseCodes, int? priceList = null, int limit = 20, int offset = 0);
}
