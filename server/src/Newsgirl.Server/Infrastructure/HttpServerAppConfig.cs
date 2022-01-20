namespace Newsgirl.Server.Infrastructure;

using Xdxd.DotNet.Logging;

public class HttpServerAppConfig
{
    public string ConnectionString { get; set; }

    public string SentryDsn { get; set; }

    public string InstanceName { get; set; }

    public string Environment { get; set; }

    /// <summary>
    /// The pfx certificate that is used to create JWT tokens.
    /// </summary>
    public string SessionCertificate { get; set; }

    public HttpServerAppLoggingConfig Logging { get; set; }
}

public class HttpServerAppLoggingConfig
{
    public EventStreamConfig[] StructuredLogger { get; set; }

    public ElasticsearchConfig Elasticsearch { get; set; }

    public HttpServerAppElkIndexConfig ElkIndexes { get; set; }
}

public class HttpServerAppElkIndexConfig
{
    public string GeneralLogIndex { get; set; }

    public string HttpLogIndex { get; set; }
}
