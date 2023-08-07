namespace Abs.CommonCore.Platform.ConfigFolderWatcher
{
    public class ConfigFolderWatcher : IConfigFolderWatcher
    {
        private static readonly string[] ConfigFilesExtenstions = { "*.json", "*.config" };
        private static readonly NotifyFilters NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

        private FileSystemWatcher? _fileSystemWatcher;
        (string, DateTime)? _newFileVerificator;
        Dictionary<string, DateTime> _filesVerificators = new();
        private readonly string[] _filters;
        private readonly NotifyFilters _notifyFilter;

        public event EventHandler<string>? Changed;
        public event EventHandler<string>? Added;
        public event EventHandler<string>? Deleted;
        public event EventHandler<Exception>? Failed;

        public ConfigFolderWatcher(string configFolderPath)
        {
            if (!Directory.Exists(configFolderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {configFolderPath}");
            }

            foreach (var filePath in Directory.GetFiles(configFolderPath))
            {
                var file = new FileInfo(filePath);
                _filesVerificators.Add(file.Name, file.LastWriteTime);
            }

            _filters = ConfigFilesExtenstions;
            _notifyFilter = NotifyFilter;

            InitFileSystemWatcher(configFolderPath);
        }

        public void Dispose()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.Created -= FileSystemWatcherOnCreated;
                _fileSystemWatcher.Changed -= FileSystemWatcherOnChanged;
                _fileSystemWatcher.Renamed -= FileSystemWatcherOnRenamed;
                _fileSystemWatcher.Deleted -= FolderWatcherOnDeleted;
                _fileSystemWatcher.Error -= FileSystemWathcerOnError;

                _fileSystemWatcher.Dispose();
            }
        }

        private FileSystemWatcher CreateFileSystemWatcher(string folderPath)
        {
            return new FileSystemWatcher(folderPath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false,
                NotifyFilter = _notifyFilter
            };
        }

        private void InitFileSystemWatcher(string folderPath)
        {
            _fileSystemWatcher = CreateFileSystemWatcher(folderPath);

            foreach (var filter in _filters)
            {
                _fileSystemWatcher.Filters.Add(filter);
            }

            _fileSystemWatcher.Created += FileSystemWatcherOnCreated;

            _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;

            _fileSystemWatcher.Renamed += FileSystemWatcherOnRenamed;

            _fileSystemWatcher.Deleted += FolderWatcherOnDeleted;

            _fileSystemWatcher.Error += FileSystemWathcerOnError;
        }

        private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);
            if (file.Exists)
            {
                _newFileVerificator = new ValueTuple<string, DateTime>(file.Name, file.LastWriteTime);

                Added?.Invoke(this, e.FullPath);
            }
        }

        private void FileSystemWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            if (!e.FullPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = e.Name ?? string.Empty;
                var oldFileName = e.OldName ?? string.Empty;

                if (_filesVerificators.ContainsKey(oldFileName))
                {
                    _filesVerificators.Remove(oldFileName);
                }

                var file = new FileInfo(e.FullPath);
                _filesVerificators[fileName] = file.LastWriteTime;

                Changed?.Invoke(this, e.FullPath);
            }
        }

        private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs e)
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

            if (!string.IsNullOrEmpty(e.Name))
            {
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
            }

            if (isLastWriteTimeChanged)
            {
                Changed?.Invoke(this, e.FullPath);
            }
        }

        private void FileSystemWathcerOnError(object sender, ErrorEventArgs e)
        {
            Failed?.Invoke(this, e.GetException());
        }

        private void FolderWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                _filesVerificators.Remove(e.Name);
            }

            Deleted?.Invoke(this, e.FullPath);
        }
    }
}
