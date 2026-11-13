namespace SAPToOdoo.Application.DTOs.Warehouse
{
    public class WarehouseSchemaResponse
    {
        public string ObjectName { get; set; } = string.Empty;
        public IReadOnlyList<WarehouseSchemaField> Fields { get; set; }
            = Array.Empty<WarehouseSchemaField>();
    }

    public class WarehouseSchemaField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
