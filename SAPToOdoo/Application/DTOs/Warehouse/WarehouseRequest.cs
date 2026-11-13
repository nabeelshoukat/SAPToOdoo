using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Warehouse
{
    public class WarehouseRequest
    {
        [Required]
        [MaxLength(8)]
        public string WhsCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string WhsName { get; set; } = string.Empty;

        [MaxLength(4)]
        public string? GrpCode { get; set; }

        [MaxLength(15)]
        public string? InventoryAccount { get; set; }

        [MaxLength(15)]
        public string? CostOfGoodsSoldAccount { get; set; }

        [MaxLength(15)]
        public string? TransferAccount { get; set; }
    }
}
