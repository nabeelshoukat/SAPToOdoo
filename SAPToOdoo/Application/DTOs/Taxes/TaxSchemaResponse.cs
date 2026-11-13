namespace SAPToOdoo.Application.DTOs.Taxes
{
    public class TaxSchemaResponse
    {
        public string ObjectName { get; set; } = string.Empty;

        public List<TaxSchemaFieldResponse> Fields { get; set; } = new();
    }

    public class TaxSchemaFieldResponse
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}
