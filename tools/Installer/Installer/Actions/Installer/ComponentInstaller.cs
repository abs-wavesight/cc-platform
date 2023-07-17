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

            _logger.LogInformation("Beginning installation phase");
            await InstallComponentsAsync();
            _logger.LogInformation("Installation phase complete");

            _logger.LogInformation("Beginning execution phase");
            await ExecuteComponentsAsync();
            _logger.LogInformation("Execution phase complete");

            _logger.LogInformation("Installer complete");
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
                .Where(x => x.Action == ActionType.Execute);

            foreach (var action in actions)
            {
                await ProcessExecuteActionAsync(action, rootLocation);
            }
        }

        private async Task ProcessInstallActionAsync(ComponentAction action, string rootLocation)
        {
            foreach (var source in action.Source)
            {
                _logger.LogInformation($"Running installation for '{source}'");
                if (source.EndsWith(".tar")) await _commandExecutionService.ExecuteCommandAsync("docker", $"load -i {source}", rootLocation);
                else
                {
                    var parts = source.Split(' ');
                    await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
                }
            }
        }

        private async Task ProcessExecuteActionAsync(ComponentAction action, string rootLocation)
        {
            foreach (var source in action.Source)
            {
                _logger.LogInformation($"Running execution for '{source}'");
                var parts = source.Split(' ');
                await _commandExecutionService.ExecuteCommandAsync(parts[0], parts.Skip(1).StringJoin(" "), rootLocation);
            }
        }
    }
}
