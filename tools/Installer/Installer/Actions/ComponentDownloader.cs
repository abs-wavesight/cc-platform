using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class ComponentDownloader
    {
        private readonly ILogger _logger;
        private readonly FileInfo _registry;

        public ComponentDownloader(ILoggerFactory loggerFactory, FileInfo registry)
        {
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();
            _registry = registry;
        }

        public async Task ExecuteAsync()
        {

        }
    }
}
