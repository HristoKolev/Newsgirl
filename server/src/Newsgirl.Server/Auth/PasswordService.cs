namespace Newsgirl.Server.Auth;

using BCrypt.Net;

public interface PasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}

public class PasswordServiceImpl : PasswordService
{
    private const HashType HASH_TYPE = HashType.SHA512;
    private const int WORK_FACTOR = 12;

    public string HashPassword(string password)
    {
        return BCrypt.EnhancedHashPassword(password, HASH_TYPE, WORK_FACTOR);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.EnhancedVerify(password, passwordHash, HASH_TYPE);
    }
}
