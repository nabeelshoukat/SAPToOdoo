using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.BusinessPartners;

public class BusinessPartnerRequest
{
    [Required]
    [MaxLength(15)]
    public string CardCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CardName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}
