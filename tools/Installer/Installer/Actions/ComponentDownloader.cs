namespace Abs.CommonCore.Installer.Actions
{
    public class ComponentDownloader
    {
        private readonly FileInfo _registry;

        public ComponentDownloader(FileInfo registry)
        {
            _registry = registry;
        }

        public async Task ExecuteAsync()
        {

        }
    }
}
