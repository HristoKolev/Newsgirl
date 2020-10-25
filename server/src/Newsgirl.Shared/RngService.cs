namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Security.Cryptography;

    public interface RngService
    {
        public string GenerateSecureString(int length);
    }

    public class RngServiceImpl : RngService
    {
        public string GenerateSecureString(int length)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var buffer = ArrayPool<byte>.Shared.Rent(length);

                try
                {
                    rngCryptoServiceProvider.GetBytes(buffer, 0, length);
                    string base64 = Convert.ToBase64String(buffer, 0, length);
                    return base64.Substring(0, length).Replace('+', '-').Replace('/', '_');
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}
