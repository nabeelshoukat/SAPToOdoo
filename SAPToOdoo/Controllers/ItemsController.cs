using Microsoft.AspNetCore.Mvc;
using SAPToOdoo.Application.DTOs.Items;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;

namespace SAPToOdoo.Controllers;

[ApiController]
[Route("api/v1/sap/items")]
public class ItemsController : ControllerBase
{
    private readonly ISapItemService _itemService;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(ISapItemService itemService, ILogger<ItemsController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] GetLatestItemsRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var items = await _itemService.GetLatestAsync(request.Limit);

        return Ok(ApiResponse<IReadOnlyList<ItemResponse>>.Ok(items, correlationId, "Latest items retrieved successfully"));
    }

    [HttpGet("by-warehouse")]
    public async Task<IActionResult> GetByWarehouse([FromQuery] GetItemsByWarehouseRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var items = await _itemService.GetByWarehouseAsync(
            request.WarehouseCode, request.Limit, request.Offset);

        return Ok(ApiResponse<IReadOnlyList<ItemResponse>>.Ok(
            items, correlationId, "Items retrieved successfully"));
    }

    [HttpGet("retail")]
    public async Task<IActionResult> GetRetail([FromQuery] GetRetailItemsRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var items = await _itemService.GetRetailAsync(
            request.ItemCode, request.WarehouseCode,
            request.PriceList, request.Limit, request.Offset);

        return Ok(ApiResponse<IReadOnlyList<RetailItemResponse>>.Ok(
            items, correlationId, "Retail items retrieved successfully"));
    }

    [HttpGet("{itemCode}")]
    public async Task<IActionResult> GetByItemCode(string itemCode)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var result = await _itemService.GetByItemCodeAsync(itemCode);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                new ApiError { Code = ErrorCodes.NotFound, Message = $"Item '{itemCode}' was not found." },
                correlationId));
        }

        return Ok(ApiResponse<ItemResponse>.Ok(result, correlationId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ItemRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var result = await _itemService.CreateAsync(request);

        _logger.LogInformation("SAP CreateItem {ItemCode} completed", result.ItemCode);

        return Ok(ApiResponse<ItemResponse>.Ok(result, correlationId, "Item created successfully"));
    }

    [HttpPut("{itemCode}")]
    public async Task<IActionResult> Update(string itemCode, [FromBody] ItemUpdateRequest request)
    {
        var correlationId = HttpContext.GetCorrelationId();

        var result = await _itemService.UpdateAsync(itemCode, request);

        if (result is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                new ApiError { Code = ErrorCodes.NotFound, Message = $"Item '{itemCode}' was not found." },
                correlationId));
        }

        return Ok(ApiResponse<ItemResponse>.Ok(result, correlationId, "Item updated successfully"));
    }
}
