using Abs.CommonCore.Installer.Actions.Downloader.Config;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging;
using Component = Abs.CommonCore.Installer.Actions.Downloader.Config.Component;

namespace Abs.CommonCore.Installer.Actions.Downloader
{
    public class ComponentDownloader
    {
        private readonly IDataRequestService _dataRequestService;
        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;
        private readonly DownloaderConfig _config;

        public ComponentDownloader(ILoggerFactory loggerFactory, IDataRequestService dataRequestService, ICommandExecutionService commandExecutionService, FileInfo config)
        {
            _dataRequestService = dataRequestService;
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();
            _config = ConfigParser.LoadConfig<DownloaderConfig>(config.FullName);
        }

        public async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(_config.OutputLocation))
            {
                throw new Exception("Output location must be specified");
            }

            _logger.LogInformation("Starting downloader");
            Directory.CreateDirectory(_config.OutputLocation);

            foreach (var component in _config.Components)
            {
                _logger.LogInformation($"Starting component '{component.Name}'");

                try
                {
                    await ExecuteComponentAsync(component);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to execute component '{component.Name}'", ex);
                }
            }

            _logger.LogInformation("Downloader complete");
        }

        private Task ExecuteComponentAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            Directory.CreateDirectory(rootLocation);

            if (component.Type == ComponentType.Docker) return ExecuteDockerComponentAsync(component);
            throw new Exception($"Unknown component type '{component.Type}'");
        }

        private async Task ExecuteDockerComponentAsync(Component component)
        {
            foreach (var file in component.Files)
            {
                await ProcessFileAsync(component, file.Type, file.Source, file.Destination);
            }
        }

        private Task ProcessFileAsync(Component component, FileType fileType, string source, string destination)
        {
            if (fileType == FileType.Container) return ProcessContainerFileAsync(component, source, destination);
            if (fileType == FileType.File) return ProcessSimpleFileAsync(component, source, destination);
            throw new Exception($"Unknown file type '{fileType}'");
        }

        private async Task ProcessContainerFileAsync(Component component, string source, string destination)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            var containerFile = Path.Combine(rootLocation, destination);

            _logger.LogInformation($"Pulling image '{source}'");
            await _commandExecutionService.ExecuteCommandAsync("docker", $"pull {source}", rootLocation);

            _logger.LogInformation($"Saving image '{source}' to '{destination}'");
            await _commandExecutionService.ExecuteCommandAsync("docker", $"save -o {containerFile} {source}", rootLocation);
        }

        private async Task ProcessSimpleFileAsync(Component component, string source, string destination)
        {
            var outputPath = Path.Combine(_config.OutputLocation, component.Name, destination);

            _logger.LogInformation($"Downloading file '{source}'");
            var data = await _dataRequestService.RequestByteArrayAsync(source);

            _logger.LogInformation($"Saving file '{source}' to '{destination}'");
            await File.WriteAllBytesAsync(outputPath, data);
        }
    }
}
