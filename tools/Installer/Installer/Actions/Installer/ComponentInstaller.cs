using Abs.CommonCore.Installer.Actions.Installer.Config;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions.Installer
{
    public class ComponentInstaller
    {
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

            _logger.LogInformation("Beginning installation phase");
            await InstallComponentsAsync();
            _logger.LogInformation("Installation phase complete");

            _logger.LogInformation("Beginning execution phase");
            await ExecuteComponentsAsync();
            _logger.LogInformation("Execution phase complete");

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

        private async Task InstallComponentsAsync()
        {
            foreach (var component in _config.Components)
            {
                _logger.LogInformation($"Installing component '{component.Name}'");

                try
                {
                    await ProcessComponentInstallersAsync(component);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to install component '{component.Name}'", ex);
                }
            }
        }

        private async Task ExecuteComponentsAsync()
        {
            foreach (var component in _config.Components)
            {
                _logger.LogInformation($"Executing component '{component.Name}'");

                try
                {
                    await ProcessComponentExecutorsAsync(component);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to execute component '{component.Name}'", ex);
                }
            }
        }

        private async Task ProcessComponentInstallersAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.Location, component.Name);
            var actions = component.Actions
                .Where(x => x.Action == ActionType.Install);

            foreach (var action in actions)
            {
                await ProcessInstallActionAsync(action, rootLocation);
            }
        }

        private async Task ProcessComponentExecutorsAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.Location, component.Name);
            var actions = component.Actions
                .Where(x => x.Action == ActionType.Execute || x.Action == ActionType.Copy);

            foreach (var action in actions)
            {
                await ProcessExecuteActionAsync(action, rootLocation);
            }
        }

        private async Task ProcessInstallActionAsync(ComponentAction action, string rootLocation)
        {
            _logger.LogInformation($"Running installation for '{action.Source}'");
            if (action.Source.EndsWith(".tar")) await _commandExecutionService.ExecuteCommandAsync("docker", $"load -i {action.Source}", rootLocation);
            else
            {
                var parts = action.Source.Split(' ');
                await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
            }
        }

        private async Task ProcessExecuteActionAsync(ComponentAction action, string rootLocation)
        {
            if (action.Action == ActionType.Copy)
            {
                _logger.LogInformation($"Copying file '{action.Source}' to '{action.Destination}'");
                var directory = Path.GetDirectoryName(action.Destination)!;
                Directory.CreateDirectory(directory);

                await _commandExecutionService.ExecuteCommandAsync("copy", $"{action.Source} {action.Destination}", rootLocation);
                return;
            }

            _logger.LogInformation($"Running execution for '{action.Source}'");
            var parts = action.Source.Split(' ');
            await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
        }
    }
}
