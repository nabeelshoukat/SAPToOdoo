using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.Taxes;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Controllers
{
    [ApiController]
    [Route("api/v1/sap/taxes")]
    public class TaxesController : ControllerBase
    {
        private readonly ISapTaxService _taxService;
        private readonly ILogger<TaxesController> _logger;

        public TaxesController(
            ISapTaxService taxService,
            ILogger<TaxesController> logger)
        {
            _taxService = taxService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? search,
            [FromQuery] string? code,
            [FromQuery] string? name,
            [FromQuery] decimal? rate,
            [FromQuery] string? validForAR,
            [FromQuery] string? validForAP,
            [FromQuery] string? locked,
            [FromQuery] int limit = 20)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _taxService.GetAsync(
                    search,
                    code,
                    name,
                    rate,
                    validForAR,
                    validForAP,
                    locked,
                    limit);

            return Ok(
                ApiResponse<IReadOnlyList<TaxResponse>>.Ok(
                    result,
                    correlationId,
                    "Tax codes retrieved successfully"));
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(
            string code)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _taxService.GetByCodeAsync(code);

            if (result is null)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Tax code '{code}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<TaxResponse>.Ok(
                    result,
                    correlationId));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] TaxRequest request)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _taxService.CreateAsync(request);

            return Ok(
                ApiResponse<TaxResponse>.Ok(
                    result,
                    correlationId,
                    "Tax code created successfully"));
        }

        [HttpPut("{code}")]
        public async Task<IActionResult> Update(
            string code,
            [FromBody] TaxUpdateRequest request)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            var result =
                await _taxService.UpdateAsync(
                    code,
                    request);

            if (result is null)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Tax code '{code}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<TaxResponse>.Ok(
                    result,
                    correlationId,
                    "Tax code updated successfully"));
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(
            string code)
        {
            var correlationId =
                HttpContext.GetCorrelationId();

            bool deleted =
                await _taxService.DeleteAsync(code);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        new ApiError
                        {
                            Code = ErrorCodes.NotFound,
                            Message =
                                $"Tax code '{code}' was not found."
                        },
                        correlationId));
            }

            return Ok(
                ApiResponse<object>.Ok(
                    new
                    {
                        Code = code
                    },
                    correlationId,
                    "Tax code deleted successfully"));
        }
    }
}
