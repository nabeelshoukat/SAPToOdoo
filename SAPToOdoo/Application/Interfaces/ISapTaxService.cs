using SAPToOdoo.Application.DTOs.Taxes;

namespace SAPToOdoo.Application.Interfaces
{
    public interface ISapTaxService
    {
        Task<IReadOnlyList<TaxResponse>> GetAsync(
        string? search,
        string? code,
        string? name,
        decimal? rate,
        string? validForAR,
        string? validForAP,
        string? locked,
        int limit);

        Task<TaxResponse?> GetByCodeAsync(
            string code);

        Task<TaxResponse> CreateAsync(
            TaxRequest request);

        Task<TaxResponse?> UpdateAsync(
            string code,
            TaxUpdateRequest request);

        Task<bool> DeleteAsync(
            string code);
    }
}
