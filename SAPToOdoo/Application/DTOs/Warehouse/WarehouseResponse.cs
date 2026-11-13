namespace SAPToOdoo.Application.DTOs.Warehouse
{
    public class WarehouseResponse
    {
        public string WhsCode { get; set; } = string.Empty;

        public string WhsName { get; set; } = string.Empty;

        public string? IntrnalKey { get; set; }

        public string? GrpCode { get; set; }

        public string? InventoryAccount { get; set; }

        public string? CostOfGoodsSoldAccount { get; set; }

        public string? TransferAccount { get; set; }

        public string? Locked { get; set; }

        public string? DataSource { get; set; }
    }
}
