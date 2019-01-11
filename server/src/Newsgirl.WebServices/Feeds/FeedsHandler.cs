namespace Newsgirl.WebServices.Feeds
{
    using System.Threading.Tasks;

    using Infrastructure;

    public class FeedsHandler
    {
        [BindRequest(typeof(RefreshFeedsRequest))]
        public async Task<ApiResult> RefreshFeeds(RefreshFeedsRequest req)
        {
            return ApiResult.SuccessfulResult();
        } 
    }

    public class RefreshFeedsRequest
    {
    }
}