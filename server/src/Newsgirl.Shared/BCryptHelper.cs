namespace Newsgirl.Shared
{
    using BCrypt.Net;

    public class BCryptHelper
    {
        private static readonly HashType HashType = HashType.SHA512;
        private static readonly int WorkFactor = 12;

        public static string CreatePassword(string password)
        {
            return BCrypt.EnhancedHashPassword(password, HashType, WorkFactor);
        }

        public static bool CheckPassword(string password, string hash)
        {
            return BCrypt.EnhancedVerify(password, hash, HashType);
        }
    }
}
