using System.ComponentModel.DataAnnotations;

namespace SAPToOdoo.Application.DTOs.Invoice
{
    public sealed class InvoiceRequest
    {
        [Required, MaxLength(15)]
        public string CardCode { get; set; } = default!;

        public DateTime? DocDate { get; set; }
        public DateTime? DocDueDate { get; set; }
        public DateTime? TaxDate { get; set; }

        [MaxLength(100)]
        public string? NumAtCard { get; set; }

        [MaxLength(254)]
        public string? Comments { get; set; }

        public string? Currency { get; set; }          // e.g. "PKR"; omit to use the BP default
        public int? SalesPersonCode { get; set; }
        public int? Series { get; set; }

        /// <summary>Header UDFs, e.g. { "U_PO": "PO-123", "U_FBR_UUID": "..." }</summary>
        public Dictionary<string, object?>? UserFields { get; set; }

        [Required, MinLength(1)]
        public List<InvoiceLineRequest> Lines { get; set; } = new();
    }

}
