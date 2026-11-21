using System.Net;

namespace ClimateSafe.Auth;

public class AuthService
{
    public bool Register(string name, string email, string password)
    {
        try { ApiClient.Post<object>("api/account/register", new { name, email, password }); return true; }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.Conflict) { return false; }
    }

    public UserAccount? Login(string email, string password)
    {
        try
        {
            ApiClient.Token = null;
            var result = ApiClient.Post<ApiLogin>("api/account/login", new { email, password })
                ?? throw new InvalidOperationException("Respons login tidak tersedia.");
            ApiClient.Token = result.Token;
            var account = result.Account;
            return account.Role == "Admin"
                ? new Admin(account.Id, account.Name, account.Email, "", account.Role, account.Status, account.CreatedAt)
                : new User(account.Id, account.Name, account.Email, "", account.Role, account.Status, account.CreatedAt);
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.Unauthorized) { return null; }
    }
}
