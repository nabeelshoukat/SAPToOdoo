using SAPToOdoo.Application.DTOs.Invoice;

namespace SAPToOdoo.Application.Interfaces
{
    public interface ISapInvoiceService
    {
        Task<InvoiceResponse> CreateAsync(InvoiceRequest request);
    }
}
