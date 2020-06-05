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

        public FileWatcher(string filePath, Action onChange, TimeSpan? throttleDuration = null)
        {
            this.onChange = DelegateHelpers.Throttle(onChange, throttleDuration ?? TimeSpan.FromSeconds(1));
            
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

        private void WatcherOnDeleted(object sender, FileSystemEventArgs e) => this.onChange();

        private void WatcherOnRenamed(object sender, RenamedEventArgs e) => this.onChange();

        private void WatcherOnCreated(object sender, FileSystemEventArgs e) => this.onChange();

        private void WatcherOnChanged(object sender, FileSystemEventArgs e) => this.onChange();

        public void Dispose()
        {
            this.fileSystemWatcher.Changed += this.WatcherOnChanged;
            this.fileSystemWatcher.Created += this.WatcherOnCreated;
            this.fileSystemWatcher.Renamed += this.WatcherOnRenamed;
            this.fileSystemWatcher.Deleted += this.WatcherOnDeleted;

            this.fileSystemWatcher?.Dispose();
        }
    }

    public static class DelegateHelpers
    {
        public static Action Throttle(Action x, TimeSpan duration)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            Timer timer = null;
            
            return () =>
            {
                semaphore.Wait();

                try
                {
                    if (timer != null)
                    {
                        return;
                    }

                    x();
            
                    timer = new Timer(s =>
                    {
                        semaphore.Wait();

                        try
                        {
                            x();
                
                            timer.Dispose();
                            timer = null;
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                    }, null, duration, duration);
                }
                finally
                {
                    semaphore.Release();
                }
            };
        }
    }
}
 