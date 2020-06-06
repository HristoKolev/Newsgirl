namespace Newsgirl.Shared
{
    using System;
    using System.IO;

    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly Action onChange;

        public FileWatcher(string filePath, Action onChange, TimeSpan? throttleDuration = null)
        {
            this.onChange = DelegateHelper.Debounce(onChange, throttleDuration ?? TimeSpan.FromSeconds(1));
            
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
}
