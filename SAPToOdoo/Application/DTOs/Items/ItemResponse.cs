namespace SAPToOdoo.Application.DTOs.Items;

public class ItemResponse
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int ItemsGroupCode { get; set; }
    public DateTime? CreateDate { get; set; }
    public string? DefaultWarehouse { get; set; }
    public string? UnitOfMeasure { get; set; }
    public List<WarehouseStockResponse> Warehouses { get; set; } = new();
}

public class WarehouseStockResponse
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal OnHand { get; set; }
}