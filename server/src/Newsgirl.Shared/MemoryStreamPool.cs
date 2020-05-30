namespace Newsgirl.Shared
{
    using Microsoft.IO;

    public static class MemoryStreamPool
    {
        public static readonly RecyclableMemoryStreamManager Shared = new RecyclableMemoryStreamManager();
    }
}
