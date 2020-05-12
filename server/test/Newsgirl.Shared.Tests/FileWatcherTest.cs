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
            const int iterationCount = 10;

            if (!File.Exists(testFilePath))
            {
                File.WriteAllText(testFilePath, "");
            }

            int changeCount = 0;

            Task OnFileChange()
            {
                Interlocked.Increment(ref changeCount);
                
                return Task.CompletedTask;
            }

            using (var watcher = new FileWatcher(testFilePath, OnFileChange, TimeSpan.FromMilliseconds(5)))
            {
                for (int i = 0; i < iterationCount; i++)
                {
                    File.WriteAllText(testFilePath, i.ToString());
                    
                    await Task.Delay(10);
                }
            }

            Assert.InRange(changeCount, 5, 15);
        }
    }
}
