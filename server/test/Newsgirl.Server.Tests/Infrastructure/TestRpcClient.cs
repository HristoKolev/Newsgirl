namespace Newsgirl.Server.Tests.Infrastructure
{
    using System.Threading.Tasks;
    using Autofac;
    using Shared;

    public class TestRpcClient : RpcClient
    {
        private readonly HttpServerApp app;

        public TestRpcClient(HttpServerApp app)
        {
            this.app = app;
        }

        protected override async Task<RpcResult<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request)
        {
            var requestMessage = new RpcRequestMessage
            {
                Payload = request,
                Type = request.GetType().Name,
            };

            var instanceProvider = this.app.IoC.Resolve<InstanceProvider>();

            var result = await this.app.RpcEngine.Execute(requestMessage, instanceProvider);

            if (result.IsOk)
            {
                return RpcResult.Ok((TResponse) result.Payload);
            }

            return RpcResult.Error<TResponse>(result.ErrorMessages);
        }
    }
}
