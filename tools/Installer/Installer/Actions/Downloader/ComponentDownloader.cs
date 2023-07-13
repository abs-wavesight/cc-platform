using Abs.CommonCore.Installer.Actions.Downloader.Config;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions.Downloader
{
    public class ComponentDownloader
    {
        private readonly ILogger _logger;
        private readonly DownloaderConfig _config;

        public ComponentDownloader(ILoggerFactory loggerFactory, FileInfo registry)
        {
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();
            _config = ConfigParser.LoadConfig<DownloaderConfig>(registry.FullName);
        }

        public async Task ExecuteAsync()
        {

        }
    }
}
