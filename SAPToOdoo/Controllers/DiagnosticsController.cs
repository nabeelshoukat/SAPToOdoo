using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.Interfaces;

namespace SAPToOdoo.Controllers
{
    [Route("api/v1/sap/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ISapDiagnosticsService _sapDiagnosticsService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            ILogger<DiagnosticsController> logger, ISapDiagnosticsService sapDiagnosticsService)
        {
            _logger = logger;
            _sapDiagnosticsService = sapDiagnosticsService;
        }

        [HttpGet("db-permissions")]
        public async Task<IActionResult> DbPermissions([FromQuery] string tables = "ONNM,OCRD,OITM")
        {
            var list = tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = await _sapDiagnosticsService.GetDbPermissionsAsync(list);
            return Ok(result);
        }
    }
}
