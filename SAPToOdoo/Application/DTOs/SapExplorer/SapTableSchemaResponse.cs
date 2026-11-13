namespace SAPToOdoo.Application.DTOs.SapExplorer
{
    public class SapTableSchemaResponse
    {
        public string TableName { get; set; } = string.Empty;

        public IReadOnlyList<SapTableFieldResponse> Fields { get; set; }
            = new List<SapTableFieldResponse>();
    }

    public class SapTableFieldResponse
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}
