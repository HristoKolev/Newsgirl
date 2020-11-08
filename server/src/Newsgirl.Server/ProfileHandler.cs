namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using Shared;

    public class ProfileHandler
    {
        [RpcBind(typeof(ProfileInfoRequest), typeof(ProfileInfoResponse))]
        public async Task<ProfileInfoResponse> ProfileInfo(ProfileInfoRequest req)
        {
            await Task.CompletedTask;
            return new ProfileInfoResponse();
        }
    }
}
