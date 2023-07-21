using Abs.CommonCore.Installer.Actions.Downloader.Config;
using Abs.CommonCore.Installer.Config;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;
using Component = Abs.CommonCore.Installer.Config.Component;

namespace Abs.CommonCore.Installer.Actions.Downloader
{
    public class ComponentDownloader : ActionBase
    {
        private readonly IDataRequestService _dataRequestService;
        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;

        private readonly DownloaderConfig? _downloaderConfig;
        private readonly ComponentRegistryConfig _registryConfig;

        public ComponentDownloader(ILoggerFactory loggerFactory, IDataRequestService dataRequestService, ICommandExecutionService commandExecutionService,
            FileInfo registryConfig, FileInfo? downloaderConfig, Dictionary<string, string> parameters)
        {
            _dataRequestService = dataRequestService;
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();

            _downloaderConfig = downloaderConfig != null
                ? ConfigParser.LoadConfig<DownloaderConfig>(downloaderConfig.FullName)
                : null;

            var mergedParameters = _downloaderConfig?.Parameters ?? new Dictionary<string, string>();
            MergeParameters(mergedParameters, parameters);

            _registryConfig = ConfigParser.LoadConfig<ComponentRegistryConfig>(registryConfig.FullName,
                (c, t) => ReplaceConfigParameters(t, mergedParameters));
        }

        public async Task ExecuteAsync(string[]? specificComponents = null)
        {
            if (string.IsNullOrWhiteSpace(_registryConfig.Location))
            {
                throw new Exception("Location must be specified");
            }

            _logger.LogInformation("Starting downloader");
            Directory.CreateDirectory(_registryConfig.Location);

            var components = DetermineComponents(_registryConfig, specificComponents, _downloaderConfig?.Components);

            await components
                .ForAllAsync(async component =>
                {
                    _logger.LogInformation($"Downloading component '{component.Name}'");

                    try
                    {
                        await ExecuteComponentAsync(component);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to download component '{component.Name}'", ex);
                    }
                });

            _logger.LogInformation("Downloader complete");
        }

        private async Task ExecuteComponentAsync(Component component)
        {
            var rootLocation = Path.Combine(_registryConfig.Location, component.Name);
            Directory.CreateDirectory(rootLocation);

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
            var rootLocation = Path.Combine(_registryConfig.Location, component.Name);
            var containerFile = Path.Combine(rootLocation, destination);

            _logger.LogInformation($"Pulling image '{source}'");
            await _commandExecutionService.ExecuteCommandAsync("docker", $"pull {source}", rootLocation);

            _logger.LogInformation($"Saving image '{source}' to '{destination}'");
            await _commandExecutionService.ExecuteCommandAsync("docker", $"save -o {containerFile} {source}", rootLocation);
        }

        private async Task ProcessSimpleFileAsync(Component component, string source, string destination)
        {
            var outputPath = Path.Combine(_registryConfig.Location, component.Name, destination);

            _logger.LogInformation($"Downloading file '{source}'");
            var data = await _dataRequestService.RequestByteArrayAsync(source);

            _logger.LogInformation($"Saving file '{source}' to '{destination}'");
            await File.WriteAllBytesAsync(outputPath, data);
        }
    }
}
