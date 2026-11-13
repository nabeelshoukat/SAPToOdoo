namespace SAPToOdoo.Application.DTOs.SapExplorer
{
    public class SapTableExplorerResponse
    {
        public string TableName { get; set; } = string.Empty;

        public int Count { get; set; }

        public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; }
            = new List<Dictionary<string, object?>>();
    }
}
