using System.Security.Cryptography;
using System.Text;

namespace ClimateSafe.Api.Services;

internal static class SessionTokens
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
