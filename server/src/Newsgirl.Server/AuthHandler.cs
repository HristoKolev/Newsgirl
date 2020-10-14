namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using LinqToDB;
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
        public async Task<RpcResult<RegisterResponse>> Register(RegisterRequest req)
        {
            var existingLogin = await this.db.Poco.Logins
                .FirstOrDefaultAsync(x => x.Username.ToLowerInvariant() == req.Email.ToLowerInvariant());

            if (existingLogin == null)
            {
                return RpcResult.Error<RegisterResponse>("Username is already taken.");
            }

            return RpcResult.Ok(new RegisterResponse());
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }

    public class RegisterResponse { }
}
