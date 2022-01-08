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
            var buffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                RandomNumberGenerator.Fill(new Span<byte>(buffer, 0, length));
                string base64 = Convert.ToBase64String(buffer, 0, length);
                return base64[..length].Replace('+', '-').Replace('/', '_');
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
