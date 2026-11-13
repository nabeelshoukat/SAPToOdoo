using SAPToOdoo.Application.DTOs.DataBase;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SAPToOdoo.Infrastructure.Sap
{
    public sealed class SapDiagnosticsService : ISapDiagnosticsService
    {
        private static readonly Regex SafeName = new("^[A-Za-z0-9_]{1,128}$", RegexOptions.Compiled);

        private readonly ISapConnectionService _connectionService;
        private readonly ILogger<SapDiagnosticsService> _logger;

        public SapDiagnosticsService(ISapConnectionService connectionService, ILogger<SapDiagnosticsService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        public Task<DbPermissionResponse> GetDbPermissionsAsync(IEnumerable<string> tables)
        {
            var tableList = tables.Where(t => SafeName.IsMatch(t)).Distinct().ToList();
            if (tableList.Count == 0)
                tableList.Add("ONNM");

            return StaTaskRunner.RunAsync(() =>
            {
                using var handle = _connectionService.Connect();
                dynamic? rs = null;
                try
                {
                    rs = handle.GetBusinessObjectFromCandidates(
                        SapObjectTypes.BoRecordsetCandidates, "check db permissions");

                    var response = new DbPermissionResponse();

                    // 1) Who am I?
                    rs.DoQuery("SELECT SUSER_SNAME() AS SqlLogin, USER_NAME() AS DbUser, DB_NAME() AS DbName");
                    if (!(bool)rs.EoF)
                    {
                        response.SqlLogin = rs.Fields.Item("SqlLogin").Value?.ToString();
                        response.DatabaseUser = rs.Fields.Item("DbUser").Value?.ToString();
                        response.DatabaseName = rs.Fields.Item("DbName").Value?.ToString();
                    }

                    // 2) Database roles of the current user
                    rs.DoQuery(@"
                    SELECT r.name AS RoleName
                    FROM sys.database_role_members m
                    JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
                    JOIN sys.database_principals u ON u.principal_id = m.member_principal_id
                    WHERE u.name = USER_NAME()");
                    while (!(bool)rs.EoF)
                    {
                        response.Roles.Add(rs.Fields.Item("RoleName").Value?.ToString() ?? "");
                        rs.MoveNext();
                    }

                    // 3) Effective permissions per table
                    foreach (var table in tableList)
                    {
                        var perms = new List<string>();
                        rs.DoQuery($"SELECT permission_name FROM fn_my_permissions('dbo.{table}', 'OBJECT')");
                        while (!(bool)rs.EoF)
                        {
                            perms.Add(rs.Fields.Item("permission_name").Value?.ToString() ?? "");
                            rs.MoveNext();
                        }
                        response.Permissions[table] = perms;
                    }

                    _logger.LogInformation("DB permission check: login={Login}, user={User}",
                        response.SqlLogin, response.DatabaseUser);

                    return response;
                }
                catch (SapException) { throw; }
                catch (Exception ex)
                {
                    throw new SapException(ErrorCodes.SapOperationFailed,
                        $"Failed to read DB permissions: {ex.Message}", innerException: ex);
                }
                finally
                {
                    if (rs is not null && Marshal.IsComObject(rs))
                        Marshal.FinalReleaseComObject(rs);
                }
            });
        }
    }
}
