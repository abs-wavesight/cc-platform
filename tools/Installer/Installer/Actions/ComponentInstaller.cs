using System.Text.Json;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class ComponentInstaller : ActionBase
    {
        private const int DefaultMaxChunkSize = 1 * 1024 * 1024 * 1024; // 1GB
        private const string ReleaseZipName = "Release.zip";
        private const string AdditionalFilesName = "AdditionalFiles";

        private readonly ILoggerFactory _loggerFactory;
        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;

        private readonly InstallerComponentInstallerConfig? _installerConfig;
        private readonly InstallerComponentRegistryConfig _registryConfig;
        private readonly Dictionary<string, string> _allParameters;

        public ComponentInstaller(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService,
            FileInfo registryConfig, FileInfo? installerConfig, Dictionary<string, string> parameters, bool promptForMissingParameters)
        {
            _loggerFactory = loggerFactory;
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentInstaller>();

            _installerConfig = installerConfig != null
                ? ConfigParser.LoadConfig<InstallerComponentInstallerConfig>(installerConfig.FullName)
                : null;

            var mergedParameters = _installerConfig?.Parameters ?? new Dictionary<string, string>();
            mergedParameters.MergeParameters(parameters);

            if (promptForMissingParameters)
            {
                ReadMissingParameters(mergedParameters);
            }

            _allParameters = mergedParameters;
            _registryConfig = ConfigParser.LoadConfig<InstallerComponentRegistryConfig>(registryConfig.FullName,
                (c, t) => t.ReplaceConfigParameters(mergedParameters));
        }

        public async Task ExecuteAsync(string[]? specificComponents = null)
        {
            if (string.IsNullOrWhiteSpace(_registryConfig.Location))
            {
                throw new Exception("Location must be specified");
            }

            _logger.LogInformation("Starting installer");
            await ExpandReleaseZipFile();

            var components = DetermineComponents(specificComponents);
            VerifySourcesPresent(components);

            var actions = components
                .Select(x => new { Component = x, x.Actions })
                .SelectMany(x => x.Actions.Select(y => new
                {
                    x.Component,
                    RootLocation = Path.Combine(_registryConfig.Location, x.Component.Name),
                    Action = y
                }))
                .OrderByDescending(x => x.Action.Action == ComponentActionAction.Copy)
                .ThenByDescending(x => x.Action.Action == ComponentActionAction.ExecuteImmediate)
                .ThenByDescending(x => x.Action.Action == ComponentActionAction.Install)
                .ThenByDescending(x => x.Action.Action == ComponentActionAction.Execute)
                .ThenByDescending(x => x.Action.Action == ComponentActionAction.UpdatePath)
                .ThenByDescending(x =>
                    x.Action.Action is ComponentActionAction.ReplaceParameters or
                        ComponentActionAction.Chunk or
                        ComponentActionAction.Unchunk or
                        ComponentActionAction.Compress or
                        ComponentActionAction.Uncompress)
                .ThenByDescending(x => x.Action.Action == ComponentActionAction.RunDockerCompose)
                .ToArray();

            foreach (var action in actions)
            {
                await ProcessExecuteActionAsync(action.Component, action.RootLocation, action.Action);
            }

            _logger.LogInformation("Installer complete");
        }

        private Component[] DetermineComponents(string[]? specificComponents)
        {
            try
            {
                if (specificComponents?.Length > 0)
                {
                    return specificComponents
                        .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                        .Distinct()
                    .ToArray();
                }

                if (_installerConfig?.Components.Count > 0)
                {
                    return _installerConfig.Components
                        .Select(x => _registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                        .Distinct()
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to determine components to use", ex);
            }

            throw new Exception("No components found to download");
        }

        private void VerifySourcesPresent(Component[] components)
        {
            var missingFiles = components
                .SelectMany(component => component.Actions, (component, action) => new { component, action })
                .Where(t => t.action.Action == ComponentActionAction.Install || t.action.Action == ComponentActionAction.Copy)
                .Select(t => Path.Combine(_registryConfig.Location, t.component.Name, t.action.Source))
                .Where(location => VerifyFileExists(location) == false)
                .ToArray();

            if (missingFiles.Any())
            {
                throw new Exception($"Required installation files are missing: {missingFiles.StringJoin(", ")}");
            }
        }

        private bool VerifyFileExists(string location)
        {
            var directory = Path.GetDirectoryName(location)!;
            var filename = Path.GetFileName(location);

            if (filename.Contains('*') || filename.Contains('?'))
            {
                var files = Directory.GetFiles(directory, filename);
                return files.Length > 0;
            }

            return File.Exists(location);
        }

        private async Task ProcessExecuteActionAsync(Component component, string rootLocation, ComponentAction action)
        {
            try
            {
                if (action.Action == ComponentActionAction.Execute || action.Action == ComponentActionAction.ExecuteImmediate) await RunExecuteCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Install) await RunInstallCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.UpdatePath) await RunUpdatePathCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Copy) await RunCopyCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.ReplaceParameters) await RunReplaceParametersCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Chunk) await RunChunkCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Unchunk) await RunUnchunkCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Compress) await RunCompressCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.Uncompress) await RunUncompressCommandAsync(component, rootLocation, action);
                else if (action.Action == ComponentActionAction.RunDockerCompose) await RunDockerComposeCommandAsync(component, rootLocation, action);
                else throw new Exception($"Unknown action command: {action.Action}");
            }
            catch (Exception ex)
            {
                var message = $"Unable to process install action. {JsonSerializer.Serialize(action)}";
                throw new Exception(message, ex);
            }
        }

        private async Task RunExecuteCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            _logger.LogInformation($"{component.Name}: Running execution for '{action.Source}'");
            var parts = action.Source.Split(' ');
            await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
        }

        private async Task RunInstallCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            _logger.LogInformation($"{component.Name}: Running installation for '{action.Source}'");
            if (action.Source.EndsWith(".tar")) await _commandExecutionService.ExecuteCommandAsync("docker", $"load -i {action.Source}", rootLocation);
            else
            {
                var parts = action.Source.Split(' ');
                await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
            }
        }

        private async Task RunUpdatePathCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            _logger.LogInformation($"{component.Name}: Adding '{action.Source}' to system path");
            var path = Environment.GetEnvironmentVariable(Constants.PathEnvironmentVariable, EnvironmentVariableTarget.Machine)
                       ?? "";

            if (path.Contains(action.Source, StringComparison.OrdinalIgnoreCase)) return;
            await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {Constants.PathEnvironmentVariable} \"%{Constants.PathEnvironmentVariable}%;{action.Source}\"", rootLocation);
        }

        private async Task RunCopyCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            _logger.LogInformation($"{component.Name}: Copying file '{action.Source}' to '{action.Destination}'");
            var directory = Path.GetDirectoryName(action.Destination)!;

            if (string.IsNullOrWhiteSpace(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            await _commandExecutionService.ExecuteCommandAsync("copy", $"\"{action.Source}\" \"{action.Destination}\"", rootLocation);
        }

        private async Task RunReplaceParametersCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var path = Path.Combine(rootLocation, action.Source);
            var text = await File.ReadAllTextAsync(path);

            foreach (var param in _allParameters)
            {
                text = text.Replace(param.Key, param.Value, StringComparison.OrdinalIgnoreCase);
            }

            await File.WriteAllTextAsync(path, text);
        }

        private async Task RunChunkCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var chunker = new DataChunker(_loggerFactory);

            var source = new FileInfo(Path.Combine(rootLocation, action.Source));
            var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
            await chunker.ChunkFileAsync(source, destination, DefaultMaxChunkSize, false);
        }

        private async Task RunUnchunkCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var chunker = new DataChunker(_loggerFactory);

            var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
            var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
            await chunker.UnchunkFileAsync(source, destination, false);
        }

        private async Task RunCompressCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var compressor = new DataCompressor(_loggerFactory);

            var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
            var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
            await compressor.CompressDirectoryAsync(source, destination, false);
        }

        private async Task RunUncompressCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var compressor = new DataCompressor(_loggerFactory);

            var source = new FileInfo(Path.Combine(rootLocation, action.Source));
            var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
            await compressor.UncompressFileAsync(source, destination, false);
        }

        private async Task RunDockerComposeCommandAsync(Component component, string rootLocation, ComponentAction action)
        {
            var configFiles = Directory.GetFiles(action.Source, "docker-compose.*.yml", SearchOption.AllDirectories);
            var envFile = Directory.GetFiles(action.Source, "environment.env", SearchOption.TopDirectoryOnly);

            var arguments = configFiles
                .Select(x => $"-f {x}")
                .StringJoin(" ");

            if (envFile.Length == 1) arguments = $"--env-file {envFile[0]} " + arguments;
            await _commandExecutionService.ExecuteCommandAsync("docker-compose", $"{arguments} up --build --detach", rootLocation);

            var containerCount = configFiles
                .Count(x => x.Contains(".root.", StringComparison.OrdinalIgnoreCase) == false);

            await WaitForDockerContainersHealthyAsync(containerCount, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(10));
        }

        private async Task WaitForDockerContainersHealthyAsync(int totalContainers, TimeSpan totalTime, TimeSpan checkInterval)
        {
            _logger.LogInformation($"Waiting for {totalContainers} containers to be healthy");
            var start = DateTime.UtcNow;
            var client = new DockerClientConfiguration()
                .CreateClient();

            while (DateTime.UtcNow.Subtract(start) < totalTime)
            {
                var containers = await LoadContainerInfoAsync(client);
                var healthyCount = 0;

                if (containers.Length == 0) _logger.LogWarning("No containers found");

                foreach (var container in containers.OrderBy(x => x.Image))
                {
                    var isHealthy = CheckContainerHealthy(container, TimeSpan.FromSeconds(30));
                    if (isHealthy)
                    {
                        healthyCount++;
                    }

                    var logLevel = isHealthy
                        ? LogLevel.Information
                        : LogLevel.Warning;

                    _logger.Log(logLevel, $"Container '{container.Name.Trim('/')}': {(isHealthy ? "Healthy" : "Unhealthy")}");
                }

                if (healthyCount == totalContainers)
                {
                    _logger.LogInformation("All containers are healthy");
                    return;
                }

                await Task.Delay(checkInterval);
            }

            _logger.LogError("Not all containers are healthy");
        }

        private async Task<ContainerInspectResponse[]> LoadContainerInfoAsync(DockerClient client)
        {
            var containers = await client.Containers
                .ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });

            var containerInfo = await containers
                .SelectAsync(async c => await client.Containers.InspectContainerAsync(c.ID));

            return containerInfo
                .ToArray();
        }

        private bool CheckContainerHealthy(ContainerInspectResponse container, TimeSpan containerHealthyTime)
        {
            var startTime = string.IsNullOrWhiteSpace(container.State.StartedAt)
                ? DateTime.MaxValue
                : DateTime.Parse(container.State.StartedAt).ToUniversalTime();

            if (container.State.Running && container.State.Restarting == false && 
                (container.State.Health != null && container.State.Health.Status == "healthy") || DateTime.UtcNow.Subtract(startTime) > containerHealthyTime)
            {
                return true;
            }

            return false;
        }

        private async Task ExpandReleaseZipFile()
        {
            _logger.LogInformation("Preparing install components");

            var current = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(current, $"{ReleaseZipName}*", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                return;
            }

            var releaseZip = new FileInfo(Path.Combine(current, ReleaseZipName));
            var installLocation = new DirectoryInfo(_registryConfig.Location);

            _logger.LogInformation("Unchunking release files");
            var chunker = new DataChunker(_loggerFactory);
            await chunker.UnchunkFileAsync(new DirectoryInfo(current), releaseZip, false);

            _logger.LogInformation("Uncompressing release file");
            var compressor = new DataCompressor(_loggerFactory);
            await compressor.UncompressFileAsync(releaseZip, installLocation, false);

            _logger.LogInformation($"Creating {AdditionalFilesName} folder");
            var path = Path.Combine(_registryConfig.Location, AdditionalFilesName);
            Directory.CreateDirectory(path);
        }
    }
}
