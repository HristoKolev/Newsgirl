namespace Newsgirl.Shared
{
    using BCrypt.Net;

    public interface PasswordService
    {
        string CreatePassword(string password);

        bool CheckPassword(string password, string hash);
    }

    public class PasswordServiceImpl : PasswordService
    {
        private static readonly HashType HashType = HashType.SHA512;
        private static readonly int WorkFactor = 12;

        public string CreatePassword(string password)
        {
            return BCrypt.EnhancedHashPassword(password, HashType, WorkFactor);
        }

        public bool CheckPassword(string password, string hash)
        {
            return BCrypt.EnhancedVerify(password, hash, HashType);
        }
    }
}
