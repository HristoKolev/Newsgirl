namespace Newsgirl.Server.Http
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Shared;

    /// <summary>
    /// Serves Rpc requests over HTTP.
    /// </summary>
    public class RpcRequestHandler
    {
        private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

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
                result = Result.Error<object>($"General RPC server error. ErrorID: {errorID}.");
            }

            try
            {
                httpRequestState.HttpContext.Response.StatusCode = 200;

                await JsonSerializer.SerializeAsync(
                    httpRequestState.HttpContext.Response.Body,
                    result,
                    SerializationOptions
                );
            }
            catch (Exception err)
            {
                await this.errorReporter.Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE");
            }
        }

        private async Task<Result<object>> ProcessRequest(HttpRequestState httpRequestState)
        {
            // Find request metadata.
            if (string.IsNullOrWhiteSpace(httpRequestState.RpcRequestType))
            {
                return Result.Error<object>("Request type is null or an empty string.");
            }

            // Read request body.
            try
            {
                httpRequestState.RpcRequestBody = await httpRequestState.HttpContext.Request.ReadToEnd();

                if (httpRequestState.RpcRequestBody.Memory.Length == 0)
                {
                    return Result.Error<object>("The HTTP request has an empty body.");
                }
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return Result.Error<object>($"Failed to read RPC request body. ErrorID: {errorID}.");
            }

            var metadata = this.rpcEngine.GetMetadataByRequestName(httpRequestState.RpcRequestType);

            if (metadata == null)
            {
                return Result.Error<object>($"No RPC handler for request. RequestType: {httpRequestState.RpcRequestType}.");
            }

            // Parse the RPC message.
            try
            {
                try
                {
                    httpRequestState.RpcRequestPayload = JsonSerializer.Deserialize(
                        httpRequestState.RpcRequestBody.Memory.Span,
                        metadata.RequestType,
                        SerializationOptions
                    );
                }
                catch (Exception err)
                {
                    long? bytePositionInLine = null;
                    long? lineNumber = null;
                    string jsonPath = null;

                    if (err is JsonException jsonException)
                    {
                        bytePositionInLine = jsonException.BytePositionInLine;
                        lineNumber = jsonException.LineNumber;
                        jsonPath = jsonException.Path;
                    }

                    throw new DetailedLogException("Failed to parse RPC body.")
                    {
                        Fingerprint = "FAILED_TO_PARSE_RPC_BODY",
                        Details =
                        {
                            {"bytePositionInLine", bytePositionInLine},
                            {"lineNumber", lineNumber},
                            {"jsonPath", jsonPath},
                        },
                    };
                }
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return Result.Error<object>($"Failed to parse RPC body. ErrorID: {errorID}.");
            }

            // Execute.
            try
            {
                var requestMessage = new RpcRequestMessage
                {
                    Type = httpRequestState.RpcRequestType,
                    Payload = httpRequestState.RpcRequestPayload,
                };

                httpRequestState.RpcResponse = await this.rpcEngine.Execute(requestMessage, this.instanceProvider);
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return Result.Error<object>($"Failed to execute RPC request. ErrorID: {errorID}.");
            }

            return httpRequestState.RpcResponse;
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
}
