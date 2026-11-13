namespace SAPToOdoo.Application.DTOs.DataBase
{
    public sealed class DbPermissionResponse
    {
        public string? SqlLogin { get; set; }          // SUSER_SNAME()
        public string? DatabaseUser { get; set; }      // USER_NAME()
        public string? DatabaseName { get; set; }      // DB_NAME()
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, List<string>> Permissions { get; set; } = new(); // table -> permissions
    }
}
