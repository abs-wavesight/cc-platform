using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class ComponentDownloader : ActionBase
    {
        private readonly IDataRequestService _dataRequestService;
        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;

        private readonly InstallerComponentDownloaderConfig? _downloaderConfig;
        private readonly InstallerComponentRegistryConfig _registryConfig;

        public ComponentDownloader(ILoggerFactory loggerFactory, IDataRequestService dataRequestService, ICommandExecutionService commandExecutionService,
            FileInfo registryConfig, FileInfo? downloaderConfig, Dictionary<string, string> parameters)
        {
            _dataRequestService = dataRequestService;
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();

            _downloaderConfig = downloaderConfig != null
                ? ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(downloaderConfig.FullName)
                : null;

            var mergedParameters = _downloaderConfig?.Parameters ?? new Dictionary<string, string>();
            MergeParameters(mergedParameters, parameters);

            _registryConfig = ConfigParser.LoadConfig<InstallerComponentRegistryConfig>(registryConfig.FullName,
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

            // Assumes all files can be processed in any order
            await component.Files
                .ForAllAsync(async file =>
                {
                    await ProcessFileAsync(component, file);
                });
        }

        private async Task ProcessFileAsync(Component component, ComponentFile file)
        {
            try
            {
                if (file.Type == ComponentFileType.Container) await ProcessContainerFileAsync(component, file.Source, file.Destination);
                if (file.Type == ComponentFileType.File) await ProcessSimpleFileAsync(component, file.Source, file.Destination);
                else throw new Exception($"Unknown file type '{file.Type}'");
            }
            catch (Exception ex)
            {
                var message = $"Unable to process download file. T:{file.Type}, S:{file.Source}, D:{file.Destination}";
                throw new Exception(message, ex);
            }
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
