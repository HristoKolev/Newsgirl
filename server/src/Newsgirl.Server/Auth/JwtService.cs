namespace Newsgirl.Server.Auth;

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Infrastructure;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Extensions.ObjectPool;
using Xdxd.DotNet.Shared;

public interface JwtService
{
    T DecodeSession<T>(string jwt) where T : class;

    string EncodeSession<T>(T session) where T : class;
}

public class JwtServiceImpl : JwtService
{
    private readonly SessionCertificatePool sessionCertificatePool;
    private readonly DateTimeService dateTimeService;
    private readonly ErrorReporter errorReporter;

    // Just to satisfy the API.
    // In reality when `X509Certificate2` is used the byte[] key is ignored.
    // The method checks it for NULL and for Length - therefore the length of 1.
    private static readonly byte[] DummyKeyArray = new byte[1];

    public JwtServiceImpl(SessionCertificatePool sessionCertificatePool, DateTimeService dateTimeService, ErrorReporter errorReporter)
    {
        this.sessionCertificatePool = sessionCertificatePool;
        this.dateTimeService = dateTimeService;
        this.errorReporter = errorReporter;
    }

    public T DecodeSession<T>(string jwt) where T : class
    {
        var cert = this.sessionCertificatePool.Get();

        try
        {
            var serializer = new JsonNetSerializer();
            var validator = new JwtValidator(serializer, new CustomDateTimeProvider(this.dateTimeService));
            var decoder = new JwtDecoder(serializer, validator, new JwtBase64UrlEncoder(), new RSAlgorithmFactory(() => cert));
            return decoder.DecodeToObject<T>(jwt, DummyKeyArray, true);
        }
        catch (Exception ex)
        {
            this.errorReporter.Error(ex, "FAILED_TO_DECODE_JWT");
            return null;
        }
        finally
        {
            this.sessionCertificatePool.Return(cert);
        }
    }

    public string EncodeSession<T>(T session) where T : class
    {
        var cert = this.sessionCertificatePool.Get();

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
            this.sessionCertificatePool.Return(cert);
        }
    }

    private class CustomDateTimeProvider : IDateTimeProvider
    {
        private readonly DateTimeService dateTimeService;

        public CustomDateTimeProvider(DateTimeService dateTimeService)
        {
            this.dateTimeService = dateTimeService;
        }

        public DateTimeOffset GetNow()
        {
            return this.dateTimeService.EventTime();
        }
    }
}

public class JwtPayload
{
    [JsonPropertyName("sub")]
    public int SessionID { get; set; }

    [JsonPropertyName("exp")]
    public long ExpirationTime { get; set; }

    [JsonPropertyName("nbf")]
    public long NotBefore { get; set; }

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("jti")]
    public int JwtID { get; set; }
}

public class SessionCertificatePool : DefaultObjectPool<X509Certificate2>
{
    private const int MAXIMUM_RETAINED = 128;

    public SessionCertificatePool(HttpServerAppConfig appConfig) :
        base(new SessionCertificatePoolPolicy(appConfig.SessionCertificate), MAXIMUM_RETAINED) { }

    private class SessionCertificatePoolPolicy : DefaultPooledObjectPolicy<X509Certificate2>
    {
        public SessionCertificatePoolPolicy(string certificateBase64)
        {
            this.certificateBytes = Convert.FromBase64String(certificateBase64);
        }

        private readonly byte[] certificateBytes;

        public override X509Certificate2 Create()
        {
            return new X509Certificate2(this.certificateBytes);
        }
    }
}
