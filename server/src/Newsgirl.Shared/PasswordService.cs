namespace Newsgirl.Shared
{
    using BCrypt.Net;

    public interface PasswordService
    {
        string HashPassword(string password);

        bool VerifyPassword(string password, string passwordHash);
    }

    public class PasswordServiceImpl : PasswordService
    {
        private static readonly HashType HashType = HashType.SHA512;
        private static readonly int WorkFactor = 12;

        public string HashPassword(string password)
        {
            return BCrypt.EnhancedHashPassword(password, HashType, WorkFactor);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.EnhancedVerify(password, passwordHash, HashType);
        }
    }
}
