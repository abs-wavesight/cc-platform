using Abs.CommonCore.Installer.Actions.Installer.Config;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions.Installer
{
    public class ComponentInstaller
    {
        private const string _pathEnvironmentVariable = "PATH";

        private readonly ICommandExecutionService _commandExecutionService;
        private readonly ILogger _logger;
        private readonly InstallerConfig _config;

        public ComponentInstaller(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService, FileInfo config)
        {
            _commandExecutionService = commandExecutionService;
            _logger = loggerFactory.CreateLogger<ComponentInstaller>();
            _config = ConfigParser.LoadConfig<InstallerConfig>(config.FullName);
        }

        public async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(_config.Location))
            {
                throw new Exception("Location must be specified");
            }

            _logger.LogInformation("Starting installer");
            VerifySourcesPresent();

            var actions = _config.Components
                .Select(x => new { Component = x, Actions = x.Actions })
                .SelectMany(x => x.Actions.Select(y => new
                {
                    Component = x.Component,
                    RootLocation = Path.Combine(_config.Location, x.Component.Name),
                    Action = y
                }))
                .OrderByDescending(x => x.Action.IsImmediate)
                .ThenByDescending(x => x.Action.Action == ActionType.Install)
                .ToArray();

            foreach (var action in actions)
            {
                await ProcessExecuteActionAsync(action.Component, action.RootLocation, action.Action);
            }

            _logger.LogInformation("Installer complete");
        }

        private void VerifySourcesPresent()
        {
            var missingFiles = _config.Components
                .SelectMany(component => component.Actions, (component, action) => new { component, action })
                .Where(t => t.action.Action == ActionType.Install || t.action.Action == ActionType.Copy)
                .Select(t => Path.Combine(_config.Location, t.component.Name, t.action.Source))
                .Where(location => File.Exists(location) == false)
                .ToArray();

            if (missingFiles.Any())
            {
                throw new Exception($"Required installation files are missing: {missingFiles.StringJoin(", ")}");
            }
        }

        private Task ProcessExecuteActionAsync(Component component, string rootLocation, ComponentAction action)
        {
            if (action.Action == ActionType.Execute) return RunExecuteCommandAsync(component, rootLocation, action);
            if (action.Action == ActionType.Install) return RunInstallCommandAsync(component, rootLocation, action);
            if (action.Action == ActionType.UpdatePath) return RunUpdatePathCommandAsync(component, rootLocation, action);
            if (action.Action == ActionType.Copy) return RunCopyCommandAsync(component, rootLocation, action);
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
            Directory.CreateDirectory(directory);

            await _commandExecutionService.ExecuteCommandAsync("copy", $"{action.Source} {action.Destination}", rootLocation);
        }
    }
}
