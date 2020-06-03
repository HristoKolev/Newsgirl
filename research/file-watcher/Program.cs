using System;

namespace file_watcher
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    static class Program
    {
        static async Task Main(string[] args)
        {
            Action onChange = () =>
            {
                Console.WriteLine($"changed... {DateTime.Now:O}");
            };
            
            var tcs = new TaskCompletionSource<object>();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                tcs.SetResult(null);
            };
            
            const string filePath = "/work/projects/Newsgirl/research/file-watcher/test.txt";
            
            using (var fw = new FileWatcher(filePath, onChange, TimeSpan.FromSeconds(1)))
            {
                Console.WriteLine("started...");
                await tcs.Task;
            }

            Console.WriteLine();
            Console.WriteLine("exiting...");
        }
    }
    
    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Action onChange;
        private readonly TimeSpan throttleDuration;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        
        private Timer timer;

        public FileWatcher(string filePath, Action onChange, TimeSpan? throttleDuration = null)
        {
            this.onChange = onChange;
            this.throttleDuration = throttleDuration ?? TimeSpan.FromSeconds(1);
            
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(filePath),
                NotifyFilter = NotifyFilters.LastWrite
                               | NotifyFilters.FileName
                               | NotifyFilters.Size
                               | NotifyFilters.CreationTime
                               | NotifyFilters.Attributes
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.Security,
                Filter = Path.GetFileName(filePath),
            };
            
            watcher.Changed += this.WatcherOnChanged;
            watcher.Created += this.WatcherOnCreated;
            watcher.Renamed += this.WatcherOnRenamed;
            watcher.Deleted += this.WatcherOnDeleted;

            watcher.EnableRaisingEvents = true;

            this.fileSystemWatcher = watcher;
        }

        private void WatcherOnDeleted(object sender, FileSystemEventArgs e) => this.Invoke();

        private void WatcherOnRenamed(object sender, RenamedEventArgs e) => this.Invoke();

        private void WatcherOnCreated(object sender, FileSystemEventArgs e) => this.Invoke();

        private void WatcherOnChanged(object sender, FileSystemEventArgs e) => this.Invoke();
        
        private void Invoke()
        {
            this.semaphore.Wait();

            try
            {
                if (this.timer != null)
                {
                    return;
                }

                this.onChange();
            
                this.timer = new Timer(s =>
                {
                    this.semaphore.Wait();

                    try
                    {
                        this.onChange();
                
                        this.timer.Dispose();
                        this.timer = null;
                    }
                    finally
                    {
                        this.semaphore.Release();
                    }

                }, null, this.throttleDuration, this.throttleDuration);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        public void Dispose()
        {
            this.fileSystemWatcher.Changed += this.WatcherOnChanged;
            this.fileSystemWatcher.Created += this.WatcherOnCreated;
            this.fileSystemWatcher.Renamed += this.WatcherOnRenamed;
            this.fileSystemWatcher.Deleted += this.WatcherOnDeleted;

            this.fileSystemWatcher?.Dispose();
        }
    }
}
 