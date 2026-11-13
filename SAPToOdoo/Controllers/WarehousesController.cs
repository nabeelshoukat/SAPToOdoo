using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.Warehouse;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Controllers
{
    [ApiController]
    [Route("api/v1/sap/warehouses")]
    public class WarehousesController : ControllerBase
    {
        private readonly ISapWarehouseService _warehouseService;
        private readonly ILogger<WarehousesController> _logger;

        public WarehousesController(
            ISapWarehouseService warehouseService,
            ILogger<WarehousesController> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? search,
            [FromQuery] string? whsCode,
            [FromQuery] string? whsName,
            [FromQuery] string? locked,
            [FromQuery] int limit = 20)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _warehouseService.GetAsync(
                    search,
                    whsCode,
                    whsName,
                    locked,
                    limit);

            return Ok(
                ApiResponse<IReadOnlyList<WarehouseResponse>>.Ok(
                    result,
                    correlationId,
                    "Warehouses retrieved successfully"));
        }

        [HttpGet("{whsCode}")]
        public async Task<IActionResult> GetByCode(
            string whsCode)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _warehouseService.GetByCodeAsync(
                    whsCode);

            if (result is null)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Warehouse '{whsCode}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<WarehouseResponse>.Ok(
                    result,
                    correlationId));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] WarehouseRequest request)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _warehouseService.CreateAsync(request);

            return Ok(
                ApiResponse<WarehouseResponse>.Ok(
                    result,
                    correlationId,
                    "Warehouse created successfully"));
        }

        [HttpPut("{whsCode}")]
        public async Task<IActionResult> Update(
            string whsCode,
            [FromBody] WarehouseUpdateRequest request)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _warehouseService.UpdateAsync(
                    whsCode,
                    request);

            if (result is null)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Warehouse '{whsCode}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<WarehouseResponse>.Ok(
                    result,
                    correlationId,
                    "Warehouse updated successfully"));
        }

        [HttpDelete("{whsCode}")]
        public async Task<IActionResult> Delete(
            string whsCode)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            bool deleted =
                await _warehouseService.DeleteAsync(
                    whsCode);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Warehouse '{whsCode}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<object>.Ok(
                    new
                    {
                        WhsCode = whsCode
                    },
                    correlationId,
                    "Warehouse deleted successfully"));
        }
    }
}
