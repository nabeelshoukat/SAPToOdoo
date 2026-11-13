using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.BusinessPartners;

public class BusinessPartnerUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string CardName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}
