using System.Text.Json;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Abs.CommonCore.Installer.Actions;

public class ComponentDownloader : ActionBase
{
    private readonly IDataRequestService _dataRequestService;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly ILogger _logger;

    private readonly InstallerComponentDownloaderConfig? _downloaderConfig;
    private readonly InstallerComponentRegistryConfig _registryConfig;
    private readonly string? _nugetEnvironmentVariable;
    private readonly Dictionary<string, string> _parameters;

    public ComponentDownloader(ILoggerFactory loggerFactory, IDataRequestService dataRequestService, ICommandExecutionService commandExecutionService,
                               FileInfo registryConfig, FileInfo? downloaderConfig, Dictionary<string, string> parameters, bool promptForMissingParameters)
    {
        _dataRequestService = dataRequestService;
        _commandExecutionService = commandExecutionService;
        _logger = loggerFactory.CreateLogger<ComponentDownloader>();
        _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);

        _downloaderConfig = downloaderConfig != null
            ? ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(downloaderConfig.FullName)
            : null;

        var mergedParameters = _downloaderConfig?.Parameters ?? new Dictionary<string, string>();
        mergedParameters.MergeParameters(parameters);

        if (promptForMissingParameters)
        {
            ReadMissingParameters(mergedParameters);
        }

        _parameters = mergedParameters;
        _registryConfig = ConfigParser.LoadConfig<InstallerComponentRegistryConfig>(registryConfig.FullName,
            (c, t) => t.ReplaceConfigParameters(mergedParameters));
    }

    public async Task ExecuteAsync(string[]? specificComponents = null)
    {
        if (string.IsNullOrWhiteSpace(_registryConfig.Location))
        {
            throw new Exception("Location must be specified");
        }

        _logger.LogInformation("Starting downloader");
        Directory.CreateDirectory(_registryConfig.Location);

        var registeredComponentsLog = $"Registered components: {_registryConfig.Components.Select(c => c.Name).ToArray().StringJoin(", ")}";
        _logger.LogInformation(registeredComponentsLog);

        var requiredComponentsLog = $"Required components: {specificComponents?.StringJoin(", ")}";
        _logger.LogInformation(requiredComponentsLog);

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
            .ForEachAsync(async file => await ProcessFileAsync(component, file));
    }

    private async Task ProcessFileAsync(Component component, ComponentFile file)
    {
        try
        {
            if (file.Type == ComponentFileType.Container)
            {
                await ProcessContainerFileAsync(component, file.Source, file.Destination);
            }
            else if (file.Type == ComponentFileType.File)
            {
                await ProcessSimpleFileAsync(component, file.Source, file.Destination);
            }
            else if (file.Type == ComponentFileType.Release)
            {
                await ProcessReleaseFileAsync(component, file.Source, file.Destination);
            }
            else
            {
                throw new Exception($"Unknown file type '{file.Type}'");
            }
        }
        catch (Exception ex)
        {
            var message = $"Unable to process download action. {JsonSerializer.Serialize(file)}";
            throw new Exception(message, ex);
        }
    }

    private async Task ProcessContainerFileAsync(Component component, string source, string destination)
    {
        var rootLocation = Path.Combine(_registryConfig.Location, component.Name);
        var containerFile = Path.Combine(rootLocation, destination);
        var directory = Path.GetDirectoryName(containerFile)!;
        Directory.CreateDirectory(directory);

        _logger.LogInformation($"Pulling image '{source}'");
        await _commandExecutionService.ExecuteCommandAsync("docker", $"pull {source}", rootLocation);

        _logger.LogInformation($"Saving image '{source}' to '{destination}'");
        await _commandExecutionService.ExecuteCommandAsync("docker", $"save -o {containerFile} {source}", rootLocation);
    }

    private async Task ProcessSimpleFileAsync(Component component, string source, string destination)
    {
        var outputPath = Path.Combine(_registryConfig.Location, component.Name, destination);
        var directory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(directory);

        _logger.LogInformation($"Downloading file '{source}'");
        var data = await _dataRequestService.RequestByteArrayAsync(source);

        _logger.LogInformation($"Saving file '{source}' to '{destination}'");
        await File.WriteAllBytesAsync(outputPath, data);
    }

    private async Task ProcessReleaseFileAsync(Component component, string source, string destination)
    {
        _logger.LogInformation($"Downloading release '{source}' to location '{destination}'");

        var outputPath = Path.Combine(_registryConfig.Location, component.Name, destination);
        var directory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(directory);

        var segments = source.GetGitHubPathSegments();

        var client = new GitHubClient(new Octokit.ProductHeaderValue(Constants.AbsHeaderValue))
        {
            Credentials = new Credentials(_nugetEnvironmentVariable)
        };

        var windowsVersion = _parameters.GetWindowsVersion();
        var releaseFilename = $"{windowsVersion}.zip";
        var release = await client.Repository.Release.Get(segments.Owner, segments.Repo, segments.Tag);
        var files = release.Assets
            .Where(x => x.Name.Contains(releaseFilename, StringComparison.OrdinalIgnoreCase))
            .Select(x => new { Url = new Uri(x.Url, UriKind.Absolute), Filename = x.Name });

        await files
            .ForEachAsync(async file =>
            {
                var response = await client.Connection.Get<byte[]>(file.Url, new Dictionary<string, string>(), "application/octet-stream");
                var outputLocation = Path.Combine(outputPath, file.Filename);

                await File.WriteAllBytesAsync(outputLocation, response.Body);
            });
    }
}
