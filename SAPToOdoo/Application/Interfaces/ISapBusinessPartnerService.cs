using SAPToOdoo.Application.DTOs.BusinessPartners;

namespace SAPToOdoo.Application.Interfaces;

public interface ISapBusinessPartnerService
{
    Task<IReadOnlyList<BusinessPartnerListResponse>> GetAsync(string? search,
                                                              string? cardCode,
                                                              string? cardName,
                                                              string? cardType,
                                                              int? limit);
    Task<BusinessPartnerResponse?> GetByCardCodeAsync(string cardCode);
    Task<BusinessPartnerResponse> CreateAsync(BusinessPartnerRequest request);
    Task<BusinessPartnerResponse?> UpdateAsync(string cardCode, BusinessPartnerUpdateRequest request);
}
