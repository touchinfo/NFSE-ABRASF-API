using System.Security.Cryptography;

namespace NFSE_ABRASF.Helpers
{
    public static class SecurityHelper
    {
        public static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static string GenerateSecureToken(int length = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, length);
        }
    }
}