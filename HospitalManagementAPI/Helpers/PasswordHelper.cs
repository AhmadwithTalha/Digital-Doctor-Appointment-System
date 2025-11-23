using BCrypt.Net;

namespace HospitalManagementAPI.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string password, int workFactor = 10)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor);

        public static bool Verify(string password, string hashed)
            => !string.IsNullOrEmpty(hashed) && BCrypt.Net.BCrypt.Verify(password, hashed);

        public static bool IsHashed(string value)
            => !string.IsNullOrEmpty(value) &&
               (value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$"));
    }
}
