namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using Shared;
    using Shared.Postgres;

    public class AuthHandler
    {
        private readonly IDbService db;

        public AuthHandler(IDbService db)
        {
            this.db = db;
        }
        
        [RpcBind(typeof(RegisterRequest), typeof(RegisterResponse))]
        public async Task<RegisterResponse> Register(RegisterRequest req)
        {
            await Task.Delay(1000);
            return new RegisterResponse();
        }

        public class RegisterRequest
        {
            public string Email { get; set; }

            public string Password { get; set; }
        }

        public class RegisterResponse { }
    }
}
