namespace Newsgirl.Shared
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Func<Task> onChange;
        private readonly TimeSpan throttleDuration;
        
        private readonly AsyncLock asyncLock = new AsyncLock();
        private DateTime lastFired;
        private Task timeoutTask;

        public FileWatcher(string filePath, Func<Task> onChange, TimeSpan? throttleDuration = null)
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
        
        private async void Invoke()
        {
            using (await this.asyncLock.Lock())
            {
                this.lastFired = DateTime.UtcNow;

                if (this.timeoutTask == null || this.timeoutTask.IsCompleted)
                {
                    
                }
                
                this.timeoutTask ??= Task.Run(async () =>
                {
                    await Task.Delay(this.throttleDuration);
                    
                    
                });
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
