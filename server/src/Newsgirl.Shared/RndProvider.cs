namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Security.Cryptography;

    public interface RngProvider
    {
        public string GenerateSecureString(int length);
    }

    public class RngProviderImpl : RngProvider
    {
        public string GenerateSecureString(int length)
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();

            var buffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                rngCryptoServiceProvider.GetBytes(buffer, 0, length);

                string result = Convert.ToBase64String(buffer, 0, length);

                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
