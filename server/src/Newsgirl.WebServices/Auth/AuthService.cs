using Newsgirl.WebServices.Infrastructure.Data;

namespace Newsgirl.WebServices.Auth
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using LinqToDB;

    using Newsgirl.WebServices.Infrastructure.Data;

    public class AuthService
    {
        public AuthService(IDbService db)
        {
            this.Db = db;
        }

        private IDbService Db { get; }

        public async Task<UserBM> GetUser(int sessionID)
        {
            var objects = await (from session in this.Db.Poco.UserSessions
                                 join user in this.Db.Poco.Users on session.UserID equals user.UserID
                                 where session.SessionID == sessionID
                                 select new
                                 {
                                     user,
                                     session
                                 }).FirstOrDefaultAsync();
            if (objects == null)
            {
                return null;
            }

            var bm = objects.user.ToBm();
            bm.Session = objects.session.ToBm();

            return bm;
        }

        public async Task<UserBM> Login(string username, string password)
        {
            var user = await this.Db.Poco.Users.Where(x => x.Username == username && x.Password == password).FirstOrDefaultAsync();

            var bm = user?.ToBm();

            if (user == null)
            {
                return null;
            }

            var session = new UserSessionPoco
            {
                UserID = user.UserID,
                LoginDate = DateTime.Now,
            };

            await this.Db.Insert(session);

            bm.Session = session.ToBm();

            return bm;
        }
    }
}