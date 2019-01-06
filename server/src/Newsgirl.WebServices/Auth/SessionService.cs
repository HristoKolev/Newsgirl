namespace Newsgirl.WebServices.Auth
{
    using Microsoft.AspNetCore.Http;

    public class SessionService
    {
        public SessionService(IHttpContextAccessor contextAccessor)
        {
            this.ContextAccessor = contextAccessor;
        }

        public RequestSession Session => this.ContextAccessor.HttpContext.GetRequestSession();

        private IHttpContextAccessor ContextAccessor { get; }
    }
}