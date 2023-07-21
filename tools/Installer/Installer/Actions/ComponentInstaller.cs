using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class ComponentInstaller : ActionBase
    {
        private const string _pathEnvironmentVariable = "PATH";

        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;

        private readonly InstallerComponentInstallerConfig? _installerConfig;
        private readonly InstallerComponentRegistryConfig _registryConfig;

        public ComponentInstaller(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService,
            FileInfo registryConfig, FileInfo? installerConfig, Dictionary<string, string> parameters)
        {
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentInstaller>();

            _installerConfig = installerConfig != null
                ? ConfigParser.LoadConfig<InstallerComponentInstallerConfig>(installerConfig.FullName)
                : null;

            var mergedParameters = _installerConfig?.Parameters ?? new Dictionary<string, string>();
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

            _logger.LogInformation("Starting installer");
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
                .Where(location => File.Exists(location) == false)
                .ToArray();

            if (missingFiles.Any())
            {
                throw new Exception($"Required installation files are missing: {missingFiles.StringJoin(", ")}");
            }
        }

        private Task ProcessExecuteActionAsync(Component component, string rootLocation, ComponentAction action)
        {
            if (action.Action == ComponentActionAction.Execute || action.Action == ComponentActionAction.ExecuteImmediate) return RunExecuteCommandAsync(component, rootLocation, action);
            if (action.Action == ComponentActionAction.Install) return RunInstallCommandAsync(component, rootLocation, action);
            if (action.Action == ComponentActionAction.UpdatePath) return RunUpdatePathCommandAsync(component, rootLocation, action);
            if (action.Action == ComponentActionAction.Copy) return RunCopyCommandAsync(component, rootLocation, action);
            throw new Exception($"Unknown action command: {action.Action}");
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
            var path = Environment.GetEnvironmentVariable(_pathEnvironmentVariable, EnvironmentVariableTarget.Machine)
                       ?? "";

            if (path.Contains(action.Source, StringComparison.OrdinalIgnoreCase)) return;
            await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {_pathEnvironmentVariable} \"%{_pathEnvironmentVariable}%;{action.Source}\"", rootLocation);
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
    }
}
