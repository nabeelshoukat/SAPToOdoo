using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.BusinessPartners;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Controllers;

[ApiController]
[Route("api/v1/sap/business-partners")]
public class BusinessPartnersController : ControllerBase
{
    private readonly ISapBusinessPartnerService _businessPartnerService;
    private readonly ILogger<BusinessPartnersController> _logger;

    public BusinessPartnersController(ISapBusinessPartnerService businessPartnerService,
        ILogger<BusinessPartnersController> logger)
    {
        _businessPartnerService = businessPartnerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] string? search,
    [FromQuery] string? cardCode,
    [FromQuery] string? cardName,
    [FromQuery] string? cardType,
    [FromQuery] int? limit)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        var result = await _businessPartnerService.GetAsync(
            search,
            cardCode,
            cardName,
            cardType,
            limit);

        stopwatch.Stop();

        _logger.LogInformation(
            "SAP GetBusinessPartners completed in {ElapsedMs}ms, Count={Count}",
            stopwatch.ElapsedMilliseconds,
            result.Count);

        return Ok(
            ApiResponse<IReadOnlyList<BusinessPartnerListResponse>>.Ok(
                result,
                correlationId));
    }

    [HttpGet("{cardCode}")]
    public async Task<IActionResult> GetByCardCode(string cardCode)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        var result = await _businessPartnerService.GetByCardCodeAsync(cardCode);

        stopwatch.Stop();
        _logger.LogInformation(
            "SAP GetBusinessPartner {CardCode} completed in {ElapsedMs}ms, found={Found}",
            cardCode, stopwatch.ElapsedMilliseconds, result is not null);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                new ApiError { Code = ErrorCodes.NotFound, Message = $"Business Partner '{cardCode}' was not found." },
                correlationId));
        }

        return Ok(ApiResponse<BusinessPartnerResponse>.Ok(result, correlationId));
    }

    [HttpPut("{cardCode}")]
    public async Task<IActionResult> Update(string cardCode, [FromBody] BusinessPartnerUpdateRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var result = await _businessPartnerService.UpdateAsync(cardCode, request);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                new ApiError { Code = ErrorCodes.NotFound, Message = $"Business Partner '{cardCode}' was not found." },
                correlationId));
        }

        _logger.LogInformation("SAP UpdateBusinessPartner {CardCode} completed", cardCode);

        return Ok(ApiResponse<BusinessPartnerResponse>.Ok(result, correlationId, "Business Partner updated successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BusinessPartnerRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        var result = await _businessPartnerService.CreateAsync(request);

        stopwatch.Stop();
        _logger.LogInformation(
            "SAP CreateBusinessPartner {CardCode} completed in {ElapsedMs}ms",
            result.CardCode, stopwatch.ElapsedMilliseconds);

        return Ok(ApiResponse<BusinessPartnerResponse>.Ok(result, correlationId, "Business Partner created successfully"));
    }
}
