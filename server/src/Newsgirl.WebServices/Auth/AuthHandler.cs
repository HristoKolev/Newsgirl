namespace Newsgirl.WebServices.Auth
{
    using System.Threading.Tasks;
    using Newsgirl.WebServices.Infrastructure;
    using System.ComponentModel.DataAnnotations;

    // ReSharper disable once UnusedMember.Global
    public class AuthHandler
    {
        public AuthHandler(AuthService authService, JwtService jwtService)
        {
            this.AuthService = authService;
            this.JwtService = jwtService;
        }

        private AuthService AuthService { get; }

        private JwtService JwtService { get; }

        [BindRequest(typeof(LoginRequest)), InTransaction]
        // ReSharper disable once UnusedMember.Global
        public async Task<ApiResult> Login(LoginRequest request)
        {
            var user = await this.AuthService.Login(request.Username, request.Password);

            if (user == null)
            {
                return ApiResult.FromErrorMessage("Wrong username/password.");
            }

            string token = this.JwtService.EncodeSession(new PublicUserModel
            {
                SessionID = user.Session.SessionID,
            });

            return ApiResult.SuccessfulResult(new LoginResponse
            {
                Token = token,
                Username = user.Username,
            });
        }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Please, enter a your password.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please, enter a your username.")]
        public string Username { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }

        public string Username { get; set; }
    }
}