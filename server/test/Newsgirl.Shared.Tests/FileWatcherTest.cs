using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Newsgirl.Shared.Tests
{
    public class FileWatcherTest
    {
        [Fact]
        public async Task FileWatcher_Calls_The_Function_When_File_Changes()
        {
            string testFilePath = Path.GetFullPath("testfile1.txt");
            const int ITERATION_COUNT = 10;

            if (!File.Exists(testFilePath))
            {
                await File.WriteAllTextAsync(testFilePath, "");
            }

            int changeCount = 0;

            void OnFileChange()
            {
                Interlocked.Increment(ref changeCount);
            }

            using (var watcher = new FileWatcher(testFilePath, OnFileChange, TimeSpan.FromMilliseconds(5)))
            {
                for (int i = 0; i < ITERATION_COUNT; i++)
                {
                    await File.WriteAllTextAsync(testFilePath, i.ToString());
                    
                    await Task.Delay(10);
                }
            }

            Assert.InRange(changeCount, 5, 15);
        }
    }
}
