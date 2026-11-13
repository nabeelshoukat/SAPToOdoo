using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.Invoice;
using SAPToOdoo.Application.Interfaces;

namespace SAPToOdoo.Controllers
{
    [Route("api/v1/sap/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly ISapInvoiceService _invoiceService;
        public InvoicesController(ISapInvoiceService invoiceService) => _invoiceService = invoiceService;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InvoiceRequest request)
        {
            var result = await _invoiceService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
