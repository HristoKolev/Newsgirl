namespace Newsgirl.Server.Infrastructure;

using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using Auth;
using Microsoft.AspNetCore.Http;
using Xdxd.DotNet.Http;
using Xdxd.DotNet.Logging;
using Xdxd.DotNet.Rpc;
using Xdxd.DotNet.Shared;

/// <summary>
/// Serves Rpc requests over HTTP.
/// </summary>
public class RpcRequestHandler
{
    private readonly RpcEngine rpcEngine;
    private readonly InstanceProvider instanceProvider;
    private readonly ErrorReporter errorReporter;

    public RpcRequestHandler(RpcEngine rpcEngine, InstanceProvider instanceProvider, ErrorReporter errorReporter)
    {
        this.rpcEngine = rpcEngine;
        this.instanceProvider = instanceProvider;
        this.errorReporter = errorReporter;
    }

    public async Task HandleRpcRequest(HttpRequestState httpRequestState)
    {
        Result<object> result;

        try
        {
            result = await this.ProcessRequest(httpRequestState);
        }
        catch (Exception err)
        {
            string errorID = await this.errorReporter.Error(err, "RPC_GENERAL_SERVER_ERROR");
            httpRequestState.HttpContext.AddErrorIdHeader(errorID);
            result = Result.Error<object>($"General RPC server error. ErrorID: {errorID}.");
        }

        try
        {
            httpRequestState.HttpContext.Response.StatusCode = 200;
            await JsonHelper.Serialize(httpRequestState.HttpContext.Response.Body, result);
        }
        catch (Exception err)
        {
            await this.errorReporter.Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE");
        }
    }

    private async Task<Result<object>> ProcessRequest(HttpRequestState httpRequestState)
    {
        // Find request metadata.
        if (string.IsNullOrWhiteSpace(httpRequestState.RpcState.RpcRequestType))
        {
            return Result.Error<object>("Request type is null or an empty string.");
        }

        // Read request body.
        try
        {
            httpRequestState.RpcState.RpcRequestBody = await httpRequestState.HttpContext.Request.ReadToEnd();

            if (httpRequestState.RpcState.RpcRequestBody.Memory.Length == 0)
            {
                return Result.Error<object>("The HTTP request has an empty body.");
            }
        }
        catch (Exception err)
        {
            string errorID = await this.errorReporter.Error(err, "FAILED_TO_READ_RPC_BODY");
            httpRequestState.HttpContext.AddErrorIdHeader(errorID);
            return Result.Error<object>($"Failed to read RPC request body. ErrorID: {errorID}.");
        }

        var metadata = this.rpcEngine.GetMetadataByRequestName(httpRequestState.RpcState.RpcRequestType);

        if (metadata == null)
        {
            return Result.Error<object>($"No RPC handler for request. RequestType: {httpRequestState.RpcState.RpcRequestType}.");
        }

        // Parse the RPC message.
        try
        {
            httpRequestState.RpcState.RpcRequestPayload = JsonHelper.Deserialize(
                httpRequestState.RpcState.RpcRequestBody.Memory.Span,
                metadata.RequestType
            );
        }
        catch (Exception err)
        {
            string errorID = await this.errorReporter.Error(err, "FAILED_TO_PARSE_RPC_BODY");
            httpRequestState.HttpContext.AddErrorIdHeader(errorID);
            return Result.Error<object>($"Failed to parse RPC body. ErrorID: {errorID}.");
        }

        // Execute.
        try
        {
            var requestMessage = new RpcRequestMessage
            {
                Type = httpRequestState.RpcState.RpcRequestType,
                Payload = httpRequestState.RpcState.RpcRequestPayload,
            };

            httpRequestState.RpcState.RpcResponse = await this.rpcEngine.Execute(requestMessage, this.instanceProvider);
        }
        catch (Exception err)
        {
            string errorID = await this.errorReporter.Error(err);
            httpRequestState.HttpContext.AddErrorIdHeader(errorID);
            return Result.Error<object>($"Failed to execute RPC request. ErrorID: {errorID}.");
        }

        return httpRequestState.RpcState.RpcResponse;
    }
}

public class RpcAuthorizationMiddleware : RpcMiddleware
{
    public const string UNAUTHORIZED_ACCESS_MESSAGE = "Unauthorized access.";

    private static readonly RpcAuthAttribute DefaultAuthAttribute = new RpcAuthAttribute
    {
        RequiresAuthentication = true,
    };

    private readonly HttpRequestState httpRequestState;

    public RpcAuthorizationMiddleware(HttpRequestState httpRequestState)
    {
        this.httpRequestState = httpRequestState;
    }

    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        var authAttribute = context.GetSupplementalAttribute<RpcAuthAttribute>() ?? DefaultAuthAttribute;

        var authResult = this.httpRequestState.AuthResult;

        bool isAuthenticated = authResult.IsAuthenticated && authResult.ValidCsrfToken;

        if (authAttribute.RequiresAuthentication && !isAuthenticated)
        {
            context.SetResponse(Result.Error(UNAUTHORIZED_ACCESS_MESSAGE));
            return;
        }

        context.SetHandlerArgument(authResult);

        await next(context, instanceProvider);
    }
}

public class RpcInputValidationMiddleware : RpcMiddleware
{
    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        var validationResult = InputValidator.Validate(context.RequestMessage.Payload);

        if (!validationResult.IsOk)
        {
            context.SetResponse(validationResult);
            return;
        }

        await next(context, instanceProvider);
    }
}

public class RpcAuthAttribute : RpcSupplementalAttribute
{
    public bool RequiresAuthentication { get; set; }
}

public class HttpRequestState : IDisposable
{
    public HttpContext HttpContext { get; set; }

    public AuthResult AuthResult { get; set; }

    public DateTime RequestStart { get; set; }

    public DateTime RequestEnd { get; set; }

    public RpcRequestState RpcState { get; set; }

    public void Dispose()
    {
        this.RpcState?.Dispose();
    }
}

public class RpcRequestState : IDisposable
{
    public string RpcRequestType { get; set; }

    public IMemoryOwner<byte> RpcRequestBody { get; set; }

    public object RpcRequestPayload { get; set; }

    public Result<object> RpcResponse { get; set; }

    public void Dispose()
    {
        this.RpcRequestBody?.Dispose();
    }
}

public static class HttpContextAppExtensions
{
    public static void AddErrorIdHeader(this HttpContext httpContext, string errorID)
    {
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.Headers["errorID"] = errorID;
        }
    }
}

public class HttpLogData : AppInfoEventData
{
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable UnusedAutoPropertyAccessor.Global

    public HttpLogData(HttpRequestState httpRequestState, bool detailedLog)
    {
        var context = httpRequestState.HttpContext;
        var connectionInfo = context.Connection;
        var httpRequest = context.Request;

        this.LocalIp = connectionInfo.LocalIpAddress + ":" + connectionInfo.LocalPort;
        this.RemoteIp = connectionInfo.RemoteIpAddress + ":" + connectionInfo.RemotePort;
        this.HttpRequestID = connectionInfo.Id;
        this.Method = httpRequest.Method;
        this.Path = httpRequest.Path.ToString();
        this.Query = httpRequest.QueryString.ToString();
        this.Protocol = httpRequest.Protocol;
        this.Scheme = httpRequest.Scheme;
        this.Aborted = context.RequestAborted.IsCancellationRequested;
        this.StatusCode = context.Response.StatusCode;

        // --------

        this.RequestStart = httpRequestState.RequestStart;
        this.RequestEnd = httpRequestState.RequestEnd;
        this.RequestDurationMs = (long)(httpRequestState.RequestEnd - httpRequestState.RequestStart).TotalMilliseconds;

        // --------

        if (httpRequestState.AuthResult != null)
        {
            this.SessionID = httpRequestState.AuthResult.SessionID;
            this.LoginID = httpRequestState.AuthResult.LoginID;
            this.ProfileID = httpRequestState.AuthResult.ProfileID;
            this.ValidCsrfToken = httpRequestState.AuthResult.ValidCsrfToken;
        }

        var rpcState = httpRequestState.RpcState;

        if (rpcState != null)
        {
            this.RpcRequestType = rpcState.RpcRequestType;
        }

        if (detailedLog)
        {
            this.HeadersJson = JsonHelper.Serialize(httpRequest.Headers.ToDictionary(x => x.Key, x => x.Value));

            if (rpcState != null)
            {
                if (rpcState.RpcRequestBody != null)
                {
                    this.RpcRequestBodyBase64 = Convert.ToBase64String(rpcState.RpcRequestBody.Memory.Span);
                }

                if (rpcState.RpcRequestPayload != null)
                {
                    this.RpcRequestPayloadJson = JsonHelper.Serialize(rpcState.RpcRequestPayload);
                }

                if (rpcState.RpcResponse != null)
                {
                    this.RpcResponseJson = JsonHelper.Serialize(rpcState.RpcResponse);
                }
            }
        }
    }

    public string LocalIp { get; set; }

    public string RemoteIp { get; set; }

    public string HttpRequestID { get; set; }

    public string Method { get; set; }

    public string Path { get; set; }

    public string Query { get; set; }

    public string Protocol { get; set; }

    public string Scheme { get; set; }

    public bool Aborted { get; set; }

    public int StatusCode { get; set; }

    public string HeadersJson { get; set; }

    // --------

    public DateTime RequestStart { get; set; }

    public DateTime RequestEnd { get; set; }

    public long RequestDurationMs { get; set; }

    // --------

    public int SessionID { get; set; }

    public int LoginID { get; set; }

    public int ProfileID { get; set; }

    public bool ValidCsrfToken { get; set; }

    // --------

    public string RpcRequestType { get; set; }

    public string RpcRequestBodyBase64 { get; set; }

    public string RpcRequestPayloadJson { get; set; }

    public string RpcResponseJson { get; set; }

    // ReSharper restore UnusedAutoPropertyAccessor.Global
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper restore MemberCanBePrivate.Global
}

public static class HttpLoggingExtensions
{
    public const string HTTP_KEY = "HTTP_REQUESTS";
    public const string HTTP_DETAILED_KEY = "HTTP_REQUESTS_DETAILED";

    public static void Http(this Log log, Func<HttpLogData> func)
    {
        log.Log(HTTP_KEY, func);
    }

    public static void HttpDetailed(this Log log, Func<HttpLogData> func)
    {
        log.Log(HTTP_DETAILED_KEY, func);
    }
}
