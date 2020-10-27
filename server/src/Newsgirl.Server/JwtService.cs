namespace Newsgirl.Server
{
    using JWT;
    using JWT.Algorithms;
    using JWT.Serializers;

    public interface JwtService
    {
        T DecodeSession<T>(string jwt);

        string EncodeSession<T>(T session);
    }

    public class JwtServiceImpl : JwtService
    {
        private readonly SystemPools systemPools;

        // Just to satisfy the API.
        // In reality when `X509Certificate2` is used the byte[] key is ignored.
        // The method checks it for NULL and for Length - therefore the length of 1.
        private static readonly byte[] DummyKeyArray = new byte[1];

        public JwtServiceImpl(SystemPools systemPools)
        {
            this.systemPools = systemPools;
        }

        public T DecodeSession<T>(string jwt)
        {
            var cert = this.systemPools.JwtSigningCertificates.Get();

            try
            {
                var serializer = new JsonNetSerializer();
                var validator = new JwtValidator(serializer, new UtcDateTimeProvider());
                var decoder = new JwtDecoder(serializer, validator, new JwtBase64UrlEncoder(), new RSAlgorithmFactory(() => cert));
                return decoder.DecodeToObject<T>(jwt, DummyKeyArray, true);
            }
            finally
            {
                this.systemPools.JwtSigningCertificates.Return(cert);
            }
        }

        public string EncodeSession<T>(T session)
        {
            var cert = this.systemPools.JwtSigningCertificates.Get();

            try
            {
                var encoder = new JwtEncoder(
                    new RS256Algorithm(cert),
                    new JsonNetSerializer(),
                    new JwtBase64UrlEncoder()
                );

                return encoder.Encode(session, DummyKeyArray);
            }
            finally
            {
                this.systemPools.JwtSigningCertificates.Return(cert);
            }
        }
    }
}
