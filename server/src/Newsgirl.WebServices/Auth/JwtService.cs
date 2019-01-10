namespace Newsgirl.WebServices.Auth
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;

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

        public JwtService(MainLogger logger)
        {
            this.Logger = logger;
        }

        private MainLogger Logger { get; }

        public T DecodeSession(string jwt)
        {
            try
            {
                var serializer = new JsonNetSerializer();
                var validator = new JwtValidator(serializer, new UtcDateTimeProvider());

                var decoder = new JwtDecoder(serializer, validator, new JwtBase64UrlEncoder(),
                                             new RSAlgorithmFactory(GetCertificate));

                return decoder.DecodeToObject<T>(jwt, DummyKeyArray, true);
            }
            catch (Exception exception)
            {
                this.Logger.LogError(exception);

                return null;
            }
        }

        public string EncodeSession(T session)
        {
            var encoder = new JwtEncoder(
                new RS256Algorithm(GetCertificate()),
                new JsonNetSerializer(),
                new JwtBase64UrlEncoder()
            );

            return encoder.Encode(session, DummyKeyArray);
        }

        private static X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(Path.Combine(Global.DataDirectory, "certificate.pfx"));
        }
    }
}