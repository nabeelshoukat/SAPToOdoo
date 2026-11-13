using SAPToOdoo.Application.DTOs.DataBase;

namespace SAPToOdoo.Application.Interfaces
{
    public interface ISapDiagnosticsService
    {
        Task<DbPermissionResponse> GetDbPermissionsAsync(IEnumerable<string> tables);
    }
}
