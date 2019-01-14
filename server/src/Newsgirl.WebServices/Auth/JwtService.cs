namespace Newsgirl.WebServices.Auth
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Infrastructure;

    using JWT;
    using JWT.Algorithms;
    using JWT.Serializers;

    public class JwtService<T>
        where T : class
    {
        // Just to satisfy the API.
        // In reality when `X509Certificate2` is used the byte[] key is ignored.
        // The method checks it for NULL and for Length - therefore the length of 1.
        // ReSharper disable once StaticMemberInGenericType
        private static readonly byte[] DummyKeyArray = new byte[1];

        public JwtService(MainLogger logger, ObjectPool<X509Certificate2> certPool)
        {
            this.Logger = logger;
            this.CertPool = certPool;
        }

        private MainLogger Logger { get; }

        private ObjectPool<X509Certificate2> CertPool { get; }

        public async Task<T> DecodeSession(string jwt)
        {
            try
            {
                var serializer = new JsonNetSerializer();
                var validator = new JwtValidator(serializer, new UtcDateTimeProvider());

                using (var certWrapper = await this.CertPool.Get())
                {
                    var decoder = new JwtDecoder(serializer, validator, new JwtBase64UrlEncoder(),
                                                 new RSAlgorithmFactory(() => certWrapper.Instance));

                    return decoder.DecodeToObject<T>(jwt, DummyKeyArray, true);
                }
            }
            catch (Exception exception)
            {
                await this.Logger.LogError(exception);

                return null;
            }
        }

        public async Task<string> EncodeSession(T session)
        {
            using (var certWrapper = await this.CertPool.Get())
            {
                var encoder = new JwtEncoder(
                    new RS256Algorithm(certWrapper.Instance),
                    new JsonNetSerializer(),
                    new JwtBase64UrlEncoder()
                );

                return encoder.Encode(session, DummyKeyArray);
            }
        }
    }
}