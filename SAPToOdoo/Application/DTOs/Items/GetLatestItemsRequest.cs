using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Items;

public class GetLatestItemsRequest
{
    [Range(1, 100, ErrorMessage = "limit must be between 1 and 100.")]
    public int Limit { get; set; } = 20;

    [MaxLength(8)]
    public string? WarehouseCode { get; set; }
}
