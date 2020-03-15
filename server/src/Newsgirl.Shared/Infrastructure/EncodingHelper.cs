using System.Text;

namespace Newsgirl.Shared.Infrastructure
{

    public static class EncodingHelper
    {
        public static readonly Encoding UTF8 = new UTF8Encoding(false, true);
    }
}