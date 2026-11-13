using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Invoice
{
    public sealed class InvoiceLineRequest
    {
        [Required, MaxLength(50)]
        public string ItemCode { get; set; } = default!;

        [Range(0.000001, double.MaxValue)]
        public double Quantity { get; set; }

        /// <summary>Unit price before discount. Omit to use the price list.</summary>
        public double? UnitPrice { get; set; }

        public double? DiscountPercent { get; set; }
        public string? WarehouseCode { get; set; }
        public string? TaxCode { get; set; }

        /// <summary>Line UDFs.</summary>
        public Dictionary<string, object?>? UserFields { get; set; }
    }
}
