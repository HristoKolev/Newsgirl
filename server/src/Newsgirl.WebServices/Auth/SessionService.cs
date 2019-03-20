namespace Newsgirl.WebServices.Auth
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Provides access to the current user session. 
    /// </summary>
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