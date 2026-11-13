using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.SapExplorer;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Controllers
{
    [ApiController]
    [Route("api/v1/sap/explorer")]
    public class SapTableExplorerController : ControllerBase
    {
        private readonly ISapTableExplorerService _explorerService;
        private readonly ILogger<SapTableExplorerController> _logger;

        public SapTableExplorerController(
            ISapTableExplorerService explorerService,
            ILogger<SapTableExplorerController> logger)
        {
            _explorerService = explorerService;
            _logger = logger;
        }

        [HttpGet("{tableName}")]
        public async Task<IActionResult> GetTable(
            string tableName,
            [FromQuery] int limit = 20)
        {
            var correlationId = HttpContext.GetCorrelationId();

            var result = await _explorerService.GetTableAsync(
                tableName,
                limit);

            _logger.LogInformation(
                "SAP Explorer table {TableName} completed. Count={Count}",
                tableName,
                result.Count);

            return Ok(
                ApiResponse<SapTableExplorerResponse>.Ok(
                    result,
                    correlationId,
                    "SAP table data retrieved successfully"));
        }

        [HttpGet("{tableName}/schema")]
        public async Task<IActionResult> GetSchema(
            string tableName)
        {
            var correlationId = HttpContext.GetCorrelationId();

            var result = await _explorerService.GetSchemaAsync(
                tableName);

            _logger.LogInformation(
                "SAP Explorer schema {TableName} completed. FieldCount={FieldCount}",
                tableName,
                result.Fields.Count);

            return Ok(
                ApiResponse<SapTableSchemaResponse>.Ok(
                    result,
                    correlationId,
                    "SAP table schema retrieved successfully"));
        }


        [HttpGet("tables")]
        public async Task<IActionResult> GetTableList()
        {
            var correlationId = HttpContext.GetCorrelationId();

            var tables = await _explorerService.GetTableListAsync();

            _logger.LogInformation(
                "SAP table list retrieved. Count={Count}",
                tables.Count);

            return Ok(
                ApiResponse<IReadOnlyList<string>>.Ok(
                    tables,
                    correlationId,
                    "SAP table list retrieved successfully"));
        }
    }
}
