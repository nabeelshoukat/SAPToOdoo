using SAPToOdoo.Application.DTOs.Warehouse;

namespace SAPToOdoo.Application.Interfaces
{
    public interface ISapWarehouseService
    {
        Task<IReadOnlyList<WarehouseResponse>> GetAsync(
         string? search,
         string? whsCode,
         string? whsName,
         string? locked,
         int limit);

        Task<WarehouseResponse?> GetByCodeAsync(string whsCode);

        Task<WarehouseResponse> CreateAsync(
            WarehouseRequest request);

        Task<WarehouseResponse?> UpdateAsync(
            string whsCode,
            WarehouseUpdateRequest request);

        Task<bool> DeleteAsync(string whsCode);
    }
}
