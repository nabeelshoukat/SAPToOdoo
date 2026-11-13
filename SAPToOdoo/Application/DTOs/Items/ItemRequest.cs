using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Items;

public class ItemRequest
{
    [Required]
    [MaxLength(20)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    public int? ItemsGroupCode { get; set; }
}
