namespace Newsgirl.Shared.Tests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class FileWatcherTest
    {
        [Fact]
        public async Task FileWatcher_Calls_The_Function_When_File_Changes()
        {
            string testFilePath = Path.GetFullPath("testfile1.txt");

            if (!File.Exists(testFilePath))
            {
                await File.WriteAllTextAsync(testFilePath, "");
            }

            int changeCount = 0;

            void OnFileChange()
            {
                Interlocked.Increment(ref changeCount);
            }

            using (var _ = new FileWatcher(testFilePath, OnFileChange, TimeSpan.FromMilliseconds(20)))
            {
                for (int i = 0; i < 10; i++)
                {
                    await File.WriteAllTextAsync(testFilePath, i.ToString());

                    await Task.Delay(1);
                }
            }

            Assert.InRange(changeCount, 1, 5);
        }
    }
}
