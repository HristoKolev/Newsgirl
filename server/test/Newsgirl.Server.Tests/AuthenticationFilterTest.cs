namespace Newsgirl.Server.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using Shared;
    using Testing;
    using Xunit;

    public class AuthenticationFilterTest
    {
        private const string COOKIE_HEADER_NAME = "Cookie";
        private const string CSRF_TOKEN_HEADER_NAME = "Csrf-Token";

        [Fact]
        public async Task Authenticate_returns_anonymous_on_null_headers()
        {
            var authenticationFilter = new AuthenticationFilter(
                CreateJwtService(),
                CreateAuthService(null),
                TestHelper.DateTimeServiceStub
            );

            var authResult = await authenticationFilter.Authenticate(null);

            AssertAnonymous(authResult);
        }

        [Fact]
        public async Task Authenticate_returns_anonymous_on_no_cookie_header()
        {
            var authenticationFilter = new AuthenticationFilter(
                CreateJwtService(),
                CreateAuthService(null),
                TestHelper.DateTimeServiceStub
            );

            var headers = new HeaderDictionary();
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Theory]
        [ClassData(typeof(FalsyStringData))]
        public async Task Authenticate_returns_anonymous_on_falsy_cookie_header(string value)
        {
            var authenticationFilter = new AuthenticationFilter(
                CreateJwtService(),
                CreateAuthService(null),
                TestHelper.DateTimeServiceStub
            );

            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, value}};
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("=x")]
        [InlineData("x=")]
        [InlineData("x=x=x")]
        public async Task Authenticate_returns_anonymous_on_non_parsable_cookie_values_header(string value)
        {
            var authenticationFilter = new AuthenticationFilter(
                CreateJwtService(),
                CreateAuthService(null),
                TestHelper.DateTimeServiceStub
            );

            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, value}};
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Fact]
        public async Task Authenticate_returns_anonymous_on_invalid_jwt()
        {
            var authenticationFilter = new AuthenticationFilter(
                CreateJwtService(),
                CreateAuthService(null),
                TestHelper.DateTimeServiceStub
            );

            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, "jwt=x"}};
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Fact]
        public async Task Authenticate_returns_anonymous_on_non_existent_session_id()
        {
            var jwtService = CreateJwtService();
            var authService = CreateAuthService(null);
            var authenticationFilter = new AuthenticationFilter(
                jwtService,
                authService,
                TestHelper.DateTimeServiceStub
            );

            string jwt = jwtService.EncodeSession(new JwtPayload {SessionID = 0});
            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, $"jwt={jwt}"}};
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Fact]
        public async Task Authenticate_returns_anonymous_on_expired_session()
        {
            var dateTimeService = TestHelper.DateTimeServiceStub;
            var jwtService = CreateJwtService();

            var session = new UserSessionPoco
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                ExpirationDate = dateTimeService.EventTime().AddSeconds(-1),
            };

            var authService = CreateAuthService(session);

            var authenticationFilter = new AuthenticationFilter(
                jwtService,
                authService,
                dateTimeService
            );

            string jwt = jwtService.EncodeSession(new JwtPayload {SessionID = 1});
            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, $"jwt={jwt}"}};
            var authResult = await authenticationFilter.Authenticate(headers);

            AssertAnonymous(authResult);
        }

        [Fact]
        public async Task Authenticate_returns_valid_session_on_no_expiration_date()
        {
            var dateTimeService = TestHelper.DateTimeServiceStub;
            var jwtService = CreateJwtService();

            var session = new UserSessionPoco
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
            };

            var authService = CreateAuthService(session);

            var authenticationFilter = new AuthenticationFilter(
                jwtService,
                authService,
                dateTimeService
            );

            string jwt = jwtService.EncodeSession(new JwtPayload {SessionID = 1});
            var headers = new HeaderDictionary {{COOKIE_HEADER_NAME, $"jwt={jwt}"}};
            var authResult = await authenticationFilter.Authenticate(headers);

            Assert.Equal(session.SessionID, authResult.SessionID);
            Assert.Equal(session.LoginID, authResult.LoginID);
            Assert.Equal(session.ProfileID, authResult.ProfileID);
        }

        [Fact]
        public async Task Authenticate_returns_non_valid_csrf_token()
        {
            var dateTimeService = TestHelper.DateTimeServiceStub;
            var jwtService = CreateJwtService();
            const string CSRF_TOKEN = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

            var session = new UserSessionPoco
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                CsrfToken = CSRF_TOKEN,
            };

            var authService = CreateAuthService(session);

            var authenticationFilter = new AuthenticationFilter(
                jwtService,
                authService,
                dateTimeService
            );

            string jwt = jwtService.EncodeSession(new JwtPayload {SessionID = 1});
            var headers = new HeaderDictionary
            {
                {COOKIE_HEADER_NAME, $"jwt={jwt}"},
                {CSRF_TOKEN_HEADER_NAME, CSRF_TOKEN + "wrong"},
            };
            var authResult = await authenticationFilter.Authenticate(headers);

            Assert.Equal(session.SessionID, authResult.SessionID);
            Assert.Equal(session.LoginID, authResult.LoginID);
            Assert.Equal(session.ProfileID, authResult.ProfileID);
            Assert.False(authResult.ValidCsrfToken);
        }

        [Fact]
        public async Task Authenticate_returns_valid_csrf_token()
        {
            var dateTimeService = TestHelper.DateTimeServiceStub;
            var jwtService = CreateJwtService();
            const string CSRF_TOKEN = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

            var session = new UserSessionPoco
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                CsrfToken = CSRF_TOKEN,
            };

            var authService = CreateAuthService(session);

            var authenticationFilter = new AuthenticationFilter(
                jwtService,
                authService,
                dateTimeService
            );

            string jwt = jwtService.EncodeSession(new JwtPayload {SessionID = 1});
            var headers = new HeaderDictionary
            {
                {COOKIE_HEADER_NAME, $"jwt={jwt}"},
                {CSRF_TOKEN_HEADER_NAME, CSRF_TOKEN},
            };
            var authResult = await authenticationFilter.Authenticate(headers);

            Assert.Equal(session.SessionID, authResult.SessionID);
            Assert.Equal(session.LoginID, authResult.LoginID);
            Assert.Equal(session.ProfileID, authResult.ProfileID);
            Assert.True(authResult.ValidCsrfToken);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void AssertAnonymous(AuthResult authResult)
        {
            Assert.Equal(0, authResult.SessionID);
            Assert.Equal(0, authResult.LoginID);
            Assert.Equal(0, authResult.ProfileID);
            Assert.False(authResult.ValidCsrfToken);
        }

        private static AuthService CreateAuthService(UserSessionPoco session)
        {
            Task<UserSessionPoco> GetSession(int sessionID)
            {
                if (session == null)
                {
                    return Task.FromResult<UserSessionPoco>(null);
                }

                if (session.SessionID == sessionID)
                {
                    return Task.FromResult(session);
                }

                return Task.FromResult<UserSessionPoco>(null);
            }

            var authService = Substitute.For<AuthService>();
            authService.GetSession(default).ReturnsForAnyArgs(x => GetSession(x.ArgAt<int>(0)));

            return authService;
        }

        private static JwtServiceImpl CreateJwtService()
        {
            var pool = new SessionCertificatePool(new SystemSettingsModel
            {
                SessionCertificate = Convert.FromBase64String(SESSION_CERTIFICATE_BASE64),
            });

            var jwtService = new JwtServiceImpl(pool, TestHelper.DateTimeServiceStub, new ErrorReporterMock());

            return jwtService;
        }

        private const string SESSION_CERTIFICATE_BASE64 =
            "MIIQOQIBAzCCD/8GCSqGSIb3DQEHAaCCD/AEgg/sMIIP6DCCBh8GCSqGSIb3DQEHBqCCBhAwggYMAgEAMIIGBQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQ" +
            "IQbI1lI9pfUgCAggAgIIF2D78UsgTiOKQ0kysXAs+94OkIXyYfpMpSACUQV7ONjBc6dElevo7bVLjSlDYjtQijE1hgGtzAIfHYP8YPDgf1f7jMo6kyoBWoB4IBT" +
            "PZ52ptViQm/biyZixwd9FKZ5EibgIGzBb1y/s8LnBYFl8zuziZoVqtA57QMbkUsoVUzkwaLvm4gHwW9UZuyrSg0Rta/5ye2k/UApR+6IUlBYRjL7gPZCXSyvdPS" +
            "qbEh/qlPgN1lGOXhXQkJV+3bJ0fa0R+U6ifY5pCNWp1vkkryDQnbvr2ql4DGlkeHTLCmOZ6zXRUens4/4jLjR28DZGVkgK7Jiri23fNeqdP+WomQ3FbmdokBLfE" +
            "3NnZGK94RZ5P/nBQr6YxMI6B9bxx6pZS2Q++XvBLaPUG6eLZjvM7EtVW66sYT8IG5MCGnOWwP3c1w6acULhuoAC5kUw6RwNFaM7c9jAej9R7EMVp5tqEALZRkmV" +
            "eDfcBQOOiOjrCJzRzy+3dddJN8Y9bHhE4go7fSvit8tjg0Dmy9rRhvX4SQUQhYOXJ5OV/sq6ilMtUGk+GrMIP6jn9EqSTvL1sozGLspyHTw9wg0KET1Z/sRbXyw" +
            "wvU5H9jZw6f3UYpfmD5KRnIwLIzo112j7sn6DIdrwkpz/ds7fIJLgl3itcPcCezU2ivJfbEFRS3MAyGk9IZDEm/2L8M4YHjDlxB40W4lhdCtqNcdMMeHffFYfFa" +
            "jIvghLVRijOXaKcNUQ57tZD0THYWge4yC887eOvkfZOwKQJRCAMsOm10+pZAfrm14dXu4/B7j95i2cDWadvYELJY204QGjblZe47Qg1ma4HogkVrxXl4XCEBzhG" +
            "bMoDNRt5ftc+9kdFHQg++6pRg2vMabITLAPnx9PFhBgo7Hh14zZTkq2lkliIRc3qZGLIFp0mHwJDD6T3ZD8oSJ6c0zA3vOcCqaTspnIWO8gg1Yk7+doRJSL3hbA" +
            "RACLi1t5CTfuh9Zkr73nU1A2MifCFBRKsITdeMcWBBBvcyBNgmbsuuoud4RHXI5kduKkL8bJUbXB7Dw2wnWwMDRTAUzzsNFtgpwf6Yo+GgyH4rrwTYA0+gGSSFl" +
            "JGOZVx+DEde91lYGLReHzKjAN+de+Cke7U/i452tX4jDMlSpA99JwKZWc9Fy32VoZ6jPPn5AceeyAFM1FsbtM4qGjzTETtjH/x7FsohexfsK1RRrxIIZBgwfxDt" +
            "Ee5GrHQN/m/CEJwxvS6vn6Kce/UJoa+vjUVv5rBsy1N/YTLf985PXOopdIQ8ASIRMyWbVT6HiqGPNW8tJ5uezwL1wFOA4axm2qLytCSayuZLqXk993tP1raXvyi" +
            "24kFDbFMCO2RlFH+hsUtYtS1YIYoRhJIQMF5L+X52pchZm6a4do0seEd0pIoLHVOU/w48dzbQqLP17r6LKcjVu36t9BGBQCt7LlvNRnPa5VbcUyvZHoOpnG52Mm" +
            "2eGGI0U+Z62WdAsZlGcy1VZiqHjjXYqAc7AqN33pelwbmzFRmaYG7dHZsi78Q35z8wUHlhut+2bLbiM9kh6pEoe/sYVoICPtNzd+n3Llz/1Uqq7/5h6ffdRR/fa" +
            "yjNt+usGxrJlCgQ/pGD1MlzvGnCIegkdhyeTPPeHIeEngomCwSXUG0KPpiWz9Z5XYoDRlvQLRpa2j94YN+XtVrr3rRaQhn6pAWRPS3jJ6Ji+3rDS2rm1/wBM16b" +
            "ipZ2TPf2gUrvurHE9rmPLA1cN7w+W8h6XXnlJrFvmLizuyu6pyZQ/B16zng3n5FjW4xl+Dq0ErnnDgc2pVAV0WTL/QuCT7JFWWx9+ZDinLrSIyAZjo/7ZlXfeOY" +
            "Tv4VEkKYHNL8fW4jRPjuTx4yr8izPbSglAl0luSq2RUVMSIhK475hVv3SRRVuArMzHlHBhy1uDRNu3vB+usxKh9puOuuTC9ufw5P1oeCYy43U2QoFTqZIBDN8Kx" +
            "XG+KWWG6yKZtmdyKxxpqVZtjJ+D/IsiPQHTHjA72VhwHuRp6JMIIJwQYJKoZIhvcNAQcBoIIJsgSCCa4wggmqMIIJpgYLKoZIhvcNAQwKAQKgggluMIIJajAcBg" +
            "oqhkiG9w0BDAEDMA4ECEHPkSABnvHJAgIIAASCCUgXWA8pzNKeO3GwvTm/OjCyannmelUGjPrwgPaq/FAaQsvy+fYGqSqJItwOKQE2kpxVbHaia7GGdcoEMC7G0" +
            "li9BFPEMvv+rYd2zt9X90PBNLELzdVVluGppu7rCyyoa9HQgxIweggKy0ivylOAXvyWCQ7WBBKS6HvAcM1JYeAIRDOWlKbtffnAvyGOTKcLddmmh+s8AktTscVw" +
            "i97wcTPW5jUIdd27PVx1dprJWPbfGPHRRfAcjPak2eHbssTNoceGbfWDJjoJWP8mXy3qkkGkuVGyzvZGvvvahwQxxL7oRyol+36az7tAGxUl1PJ9ratTM/7m9db" +
            "6eLn0YEJZcZ5/NqlNovDRwhbHVH3t74vhA20Fao5jqog+4JSPpx6uW8xGNE5a0cI+ZtU7jJSbf325W0W66zM2hXodD0e7Hzy7r+tBYmaxcpLuqF2a9iDilKQsoE" +
            "nMGUWWrN+p459lKFX1vEB1OPnl+4oZX+elaTZDjp6IAi9Bmyc725+C+NJmvdx4QyK5UBKJj8nlZmImLqpxtRd/Ek96iNWrvC7Xt6b8x2JnOfSBCceJ5yeSAQI/G" +
            "99Jb8ydrel/AYRK2Ny8eaA+9AaRkXhdlzUDBqYHIHWw5gn2X3bs49XjadPSCXyCx64YTVtPtGl+v47frmwNgZoQSbCAFh1YunYZFGwOvp0I+xEWFnUYD65ZzgpQ" +
            "QJXyUc6upeHGNoj+a4R5ctjiafXk+zQA7y6YrNadV8Pom1alRO0G00VWoDs+2dHP8niaQ09zJA0YSQpqfaNaqXZZ7mY/ZpZ8Ztg7wTcHd1zdxYgWhVr84JU53so" +
            "83lKPmBAqmwAeVRLnm0fS/NrdAtRpySFe30HywxoOJ+drKT4rEY9yd8P12DpLRfO0nfl3MaN4RTfXWCfhIRfFK90M7i9/dIbwu5/9ST/A3xgXkWdav67CeambRN" +
            "P0GJcALq/cxwJI4emfQqGcbK8yjdoRwe9Q+lZNv8y9+ntV1seXqf7o6pNJWuhEX/UZBEsIZGC/dkroSBUWsFBfdlQFhG2g8MKkjsN7UAMw1Dz4vbsqzStlecPzP" +
            "MTtzl57KbemQf8P91bAWLm0u3o7XEcEDuRllozUTQ+3O3//7THoQt2rjnvAYWUqUcZK9JIe0JAoXIhTpwU10YNScKE9UoHL0owhXshyCrIKNM4D+IdBlBONvEYC" +
            "90hyuToou1vxe/1UmQT081El1MX9AgjLU5nnYiG39U03z/PW1x0oHiVdkmRHxlsEFOpumIBYE5OITNmL/2fjr52+N5lMjr/xNEckXuo76UaVHFTLbcP8/1QX4dT" +
            "lxayrXEFV6H6MhCzUwfxwM0j7jRknSx6N8UzId1rczal+h+OeUe8AQbdYZxTA5UHKn3qS18VhQhqdqpov4XTCbncGam/LsbQA1cNwbc4IfUTyeEA+Hl0oVbRdEI" +
            "OKCqLIHV/de6ssdYOD9WzWtb0HEINin4CeOM5t2zOEPtYT0/nZYvCVrrxihwKRWVP+BKZ4tXOeAw3ngG3qXVAOGfMumN9maxqXK0QK7Oq5sUWuTZrO4MdS+5lSD" +
            "KYyQ7n2KC7HGVUiAHiW7vnl9TM6o7d9BK3Gotna/NbEHdW8vz9Siq8Ja2H6cXOMPvbJ/V66wyAc1cPMC33/OoJTipjxDUhgknPEEpMNXNobYIhlgl0C/FoL8wW4" +
            "iLBQ7JTsb0PMxyP65oF0ljKsTNmE6Pr9iKzWwfBeNkZYK/x7r2UEn+Na0I/XhNkqfElMIVvxoXfqnEpkxHvX+1tKi+n+XDAZpDnDeTbybAyY1ueMCvw62JgcIsA" +
            "JHyAOpByjbZX+n1efHCbJIRPibzg2XA3GiZh1ZlCk/l36pNH4e8IKNTuZ46brctnUWatuo1Gt4fOglLSz6XiN4z/ran0PjvtSGcDt7Orpwy4v0PoouzlN1/FHQO" +
            "VVQpGo42/wn5MXOT8ltcXUVKmYh2sxa7X7sdy10aMhUL+KDzaPGohbFdRagqasyMP7ODrCBLzFZqxN7reH76UetupZmPK+Y9ZWdJJC9KTAaf9WJTE7vX5eIsDJ5" +
            "S7HY/CEanbpxUS2+YrqONlLaQV/mQe4ycm3XM1A+xgK33tg8uU63NcKRoKeYkIcvchTEadUVw5L+EMMFyPROuScA+CmX/3juA2mzqIyEHyT5bgXKJgQWbi5ZWGw" +
            "pRQYQUTw25HZM+YhnIVmCowYcrY179M6inggyM/RSAA08Mjt4OCznuCrWfMezgOtannNr9UdzuE8NogcxV4ubYmq1JzAlrvxA7Xumq+xlg4LgPCTGBucgaEhQbg" +
            "R8W9mSBnRrUS3OD7QYLz/jVqCJ6w588Ty1DJvpfGDoJduPfYuubTSrys0yTNFEw+4mbndpQ1oFFNLB+/B5tpbqf6kKPhJMupgmyPQPzZRnjrymniRI1+iELBni0" +
            "8NQsoUcBfUKkwFUXu8m5IHyV4ScYC7Y2vzs72kdvaG0KJZ0vIg7ABrtOZic8M1WNAcrkIy0yCzuTtfgUYBeBXmsWYTzBQJkD8BT/6e2mYfgDk+jKC5UN6SEK8hT" +
            "k6BALunpcJUAB8QWyP7mwCFuYYb9b3ayEVn4RIo6jYstgz8Ibrzq+Wott6a6DbAuW0aXs7egoR90cWWt1we3mSBVzauq5HNJZCeKOXGn6l1RLlnRsA2r7ePStAi" +
            "Xm/WVDIAgWtWT9ceH6Z/SlQVCNuog+djzNOTXnlVYIgOc7f2cN8kEfofxk1btxXE5fwoA+hr+BonTnvSgaj2kIQGjg2M4A8xeiDpD4+Zb73954qRQ3uF+G2mbdy" +
            "u3LaqZoeGbIYFmWBnR1tpFVR4yTqJqnhphITm1gkBcDGDSCrR34FaqxAFQaSAY0yx7+CXEtT7FJAWCPnkrgRIIVwXsAPnswtYqdu70gR+bNjclZEs8u37wKk3uR" +
            "0yyB9roUCHrc1BIyUTnZdN/x9nxXnAD1nXMlj3mxGFM0IbqHUy++rLXmwEfd2P70vyZ/yywegFOHTXzikJYBGdzA7hz2s9hQ60DtDBckGuP96nmdgFToPPELRtG" +
            "0tS4P+pQkL9dJN4GhOM6mGKP13LxN0o/+278xWMj087e2OaVg2iDKsrzJc+8c4zhAc4iWXtZAipu6GThfj7KFkMk/l/U+tKwDseP9BxvOoria/cSuINlhjbhKW0" +
            "IWUK2ug+co0xJTAjBgkqhkiG9w0BCRUxFgQU+UitpxTZq2kZQMyActVVYS1PcbUwMTAhMAkGBSsOAwIaBQAEFBJuGg1PmGNgA80XIFApaGdCfwkyBAgWQPJYqHD" +
            "MPAICCAA=";
    }
}
