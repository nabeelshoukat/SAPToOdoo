using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Items
{
    public sealed class GetItemsByWarehouseRequest
    {
        /// <summary>
        /// One or more warehouse codes: ?warehouseCode=01&amp;warehouseCode=07
        /// (a comma-separated value such as ?warehouseCode=01,07 also works).
        /// </summary>
        [MinLength(1, ErrorMessage = "At least one warehouseCode is required.")]
        public List<string> WarehouseCode { get; set; } = new();

        [Range(1, 100)]
        public int Limit { get; set; } = 20;

        [Range(0, int.MaxValue)]
        public int Offset { get; set; } = 0;
    }
}
