namespace Abs.CommonCore.Platform.ConfigFolderWatcher
{
    public class CustomFileSystemWatcher : ICustomFileSystemWatcher
    {
        private FileSystemWatcher? _fileSystemWatcher;
        (string, DateTime)? _newFileVerificator;
        Dictionary<string, DateTime> _filesVerificators = new();
        private readonly string[] _filters;
        private readonly NotifyFilters _notifyFilters;

        public CustomFileSystemWatcher(string folderPath, string[]? filters = default, NotifyFilters notifyFilters = NotifyFilters.FileName | NotifyFilters.LastWrite)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            foreach (var filePath in Directory.GetFiles(folderPath))
            {
                var file = new FileInfo(filePath);
                _filesVerificators.Add(file.Name, file.LastWriteTime);
            }

            _filters = filters ?? new[] { "*.*" };
            _notifyFilters = notifyFilters;

            InitFileSystemWatcher(folderPath);
        }

        public event EventHandler<string>? Changed;
        public event EventHandler<string>? Added;
        public event EventHandler<string>? Deleted;
        public event EventHandler<Exception>? Failed;

        private FileSystemWatcher CreateFileSystemWatcher(string folderPath)
        {
            return new FileSystemWatcher(folderPath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
                NotifyFilter = _notifyFilters
            };
        }

        private void InitFileSystemWatcher(string folderPath)
        {
            _fileSystemWatcher = CreateFileSystemWatcher(folderPath);

            foreach (var filter in _filters)
            {
                _fileSystemWatcher.Filters.Add(filter);
            }

            _fileSystemWatcher.Created += (_, eventArgs) =>
            {
                var file = new FileInfo(eventArgs.FullPath);
                if (file.Exists)
                {
                    _newFileVerificator = new ValueTuple<string, DateTime>(file.Name, file.LastWriteTime);

                    Added?.Invoke(this, eventArgs.FullPath);
                }
            };

            _fileSystemWatcher.Changed += FolderWatcherOnChanged;

            _fileSystemWatcher.Renamed += (_, eventArgs) =>
            {
                if (!eventArgs.FullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = eventArgs.Name ?? "";
                    var oldFileName = eventArgs.OldName ?? "";

                    if (_filesVerificators.ContainsKey(oldFileName))
                    {
                        _filesVerificators.Remove(oldFileName);
                    }

                    var file = new FileInfo(eventArgs.FullPath);
                    _filesVerificators[fileName] = file.LastWriteTime;

                    Changed?.Invoke(this, eventArgs.FullPath);
                }
            };

            _fileSystemWatcher.Deleted += (_, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Name))
                {
                    _filesVerificators.Remove(eventArgs.Name);
                }

                Deleted?.Invoke(this, eventArgs.FullPath);
            };

            _fileSystemWatcher.Error += (_, eventArgs) =>
            {
                Failed?.Invoke(this, eventArgs.GetException());
            };
        }
        private void FolderWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);
            var isLastWriteTimeChanged = true;

            if (_newFileVerificator != null)
            {
                if (_newFileVerificator.Value.Item1.Equals(e.Name))
                {
                    if (file.Exists)
                    {
                        isLastWriteTimeChanged = false;

                        if (file.LastWriteTime.Equals(_newFileVerificator.Value.Item2))
                        {
                            _filesVerificators.Add(e.Name, file.LastWriteTime);
                            _newFileVerificator = null;
                        }
                    }
                }
            }

            if (_filesVerificators.TryGetValue(e.Name, out DateTime lastWriteTime))
            {
                if (file.LastWriteTime.Ticks - lastWriteTime.Ticks <= 1000)
                {
                    isLastWriteTimeChanged = false;
                }

                _filesVerificators[e.Name] = file.LastWriteTime;
            }
            else
            {
                _filesVerificators.Add(e.Name, file.LastWriteTime);
            }

            if (isLastWriteTimeChanged)
            {
                Changed?.Invoke(this, e.FullPath);
            }
        }

        public void Dispose()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Dispose();
            }
        }
    }
}
