namespace Abs.CommonCore.Platform.ConfigFolderWatcher
{
    public class ConfigFolderWatcher : CustomFileSystemWatcher
    {
        private static readonly string[] ConfigFilesExtenstions = { "*.json", "*.config" };
        private static readonly NotifyFilters NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

        public ConfigFolderWatcher(string configFolder) : base(configFolder, ConfigFilesExtenstions, NotifyFilter)
        { }
    }
}
