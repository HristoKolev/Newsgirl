namespace Newsgirl.Shared
{
    using System;
    using System.Threading.Tasks;

    public class RequestInfoMiddleware : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            var dateProvider = instanceProvider.Get<DateProvider>();

            var now = dateProvider.Now();

            var requestInfo = new RequestInfo
            {
                RequestTime = now,
            };

            context.SetHandlerArgument(requestInfo);

            await next(context, instanceProvider);
        }
    }

    public class RequestInfo
    {
        public DateTime RequestTime { get; set; }
    }
}
