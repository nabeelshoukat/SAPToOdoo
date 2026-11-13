namespace SAPToOdoo.Application.DTOs.Invoice
{
    public sealed class InvoiceResponse
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public DateTime? DocDate { get; set; }
        public string? Currency { get; set; }
        public decimal DocTotal { get; set; }
        public decimal VatSum { get; set; }
    }
}
