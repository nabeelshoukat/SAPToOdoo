using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Infrastructure.Sap;

namespace SAPToOdoo.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ISapConnectionService _connectionService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ISapConnectionService connectionService, ILogger<HealthController> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get() => Ok(new { success = true, message = "API is running" });

    [HttpGet("sap")]
    public IActionResult GetSap()
    {
        try
        {
            using var handle = _connectionService.Connect();
            return Ok(new { success = true, message = "SAP DI API connection successful" });
        }
        catch (SapException ex)
        {
            _logger.LogWarning(ex, "SAP health check failed: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "Unable to connect to SAP",
                errorCode = ex.Code
            });
        }
    }
}
