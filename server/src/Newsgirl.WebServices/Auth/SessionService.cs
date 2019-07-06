namespace Newsgirl.WebServices.Auth
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Provides access to the current user session. 
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SessionService
    {
        public SessionService(IHttpContextAccessor contextAccessor)
        {
            this.ContextAccessor = contextAccessor;
        }

        // ReSharper disable once UnusedMember.Global
        public RequestSession Session => this.ContextAccessor.HttpContext.GetRequestSession();

        private IHttpContextAccessor ContextAccessor { get; }
    }
}