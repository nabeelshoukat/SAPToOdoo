using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Items;

public class ItemUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;
}
