namespace ClimateSafe.Auth;

public class UserRepository
{
    public bool UpdateName(int userId, string name)
    {
        ApiClient.Patch<ApiAccount>("api/account/me", new { name });
        return true;
    }
}
