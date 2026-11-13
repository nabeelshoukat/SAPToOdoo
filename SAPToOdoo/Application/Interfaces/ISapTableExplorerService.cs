using SAPToOdoo.Application.DTOs.SapExplorer;

namespace SAPToOdoo.Application.Interfaces
{
    public interface ISapTableExplorerService
    {
        Task<SapTableExplorerResponse> GetTableAsync(
            string tableName,
            int limit);

        Task<SapTableSchemaResponse> GetSchemaAsync(
            string tableName);

        Task<IReadOnlyList<string>> GetTableListAsync();
    }
}
