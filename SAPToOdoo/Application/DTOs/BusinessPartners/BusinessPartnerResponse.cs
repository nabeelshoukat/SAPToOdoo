namespace SAPToOdoo.Application.DTOs.BusinessPartners;

public class BusinessPartnerResponse
{
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
