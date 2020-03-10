using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Newsgirl.Shared.Infrastructure
{
    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly IDisposable subscription;
        
        public FileWatcher(string filePath, Func<Task> onChange)
        {
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

            var changed = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler => (sender, args) => handler(args),
                handler => watcher.Changed += handler,
                handler => watcher.Changed -= handler
            );

            var created = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler => (sender, args) => handler(args),
                handler => watcher.Created += handler,
                handler => watcher.Created -= handler
            );

            var renamed = Observable.FromEvent<RenamedEventHandler, FileSystemEventArgs>(
                handler => (sender, args) => handler(args),
                handler => watcher.Renamed += handler,
                handler => watcher.Renamed -= handler
            );

            var deleted = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                handler => (sender, args) => handler(args),
                handler => watcher.Deleted += handler,
                handler => watcher.Deleted -= handler
            );

            var subscriptionHandle = changed.Merge(created)
                   .Merge(renamed)
                   .Merge(deleted)
                   .Throttle(TimeSpan.FromSeconds(1))
                   .Subscribe(async x => await onChange());

            watcher.EnableRaisingEvents = true;

            this.fileSystemWatcher = watcher;
            this.subscription = subscriptionHandle;
        }

        public void Dispose()
        {
            this.subscription?.Dispose();
            this.fileSystemWatcher?.Dispose();
        }
    }
}
