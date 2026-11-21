using System.Data;
using System.Net;

namespace ClimateSafe.Auth;

internal sealed class AdminService
{
    public DataTable GetUsers(UserAccount actor)
    {
        var users = ApiClient.Get<List<ApiAccount>>("api/admin/users") ?? new();
        var table = new DataTable();
        table.Columns.Add("user_id", typeof(int)); table.Columns.Add("name", typeof(string));
        table.Columns.Add("email", typeof(string)); table.Columns.Add("role", typeof(string));
        table.Columns.Add("status", typeof(string)); table.Columns.Add("created_at", typeof(DateTime));
        foreach (var user in users) table.Rows.Add(user.Id, user.Name, user.Email, user.Role, user.Status, user.CreatedAt);
        return table;
    }
    public bool UpdateName(UserAccount actor, int id, string name) => Change(() => ApiClient.Patch<object>($"api/admin/users/{id}/name", new { name }));
    public bool DeleteUser(UserAccount actor, int id) => Change(() => ApiClient.Delete<object>($"api/admin/users/{id}"));
    public bool ResetPassword(UserAccount actor, int id, string password) => Change(() => ApiClient.Put<object>($"api/admin/users/{id}/password", new { password }));
    public string? ToggleStatus(UserAccount actor, int id)
    {
        return Change(() => ApiClient.Post<object>($"api/admin/users/{id}/toggle-status")) ? "diperbarui" : null;
    }
    private static bool Change(Func<object?> action)
    {
        try { action(); return true; }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound) { return false; }
    }
}
