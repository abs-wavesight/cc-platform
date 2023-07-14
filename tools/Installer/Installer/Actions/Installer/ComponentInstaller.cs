using Abs.CommonCore.Installer.Actions.Installer.Config;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
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
            if (string.IsNullOrWhiteSpace(_config.OutputLocation))
            {
                throw new Exception("Output location must be specified");
            }

            _logger.LogInformation("Starting installer");

            _logger.LogInformation("Beginning installation phase");
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
            _logger.LogInformation("Installation phase complete");

            _logger.LogInformation("Beginning execution phase");
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
            _logger.LogInformation("Execution phase complete");

            _logger.LogInformation("Installer complete");
        }

        private async Task ProcessComponentInstallersAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            var actions = component.Actions
                .Where(x => x.Action == ActionType.Install);

            foreach (var action in actions)
            {
                await ProcessInstallActionAsync(action, rootLocation);
            }
        }

        private async Task ProcessComponentExecutorsAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            var actions = component.Actions
                .Where(x => x.Action == ActionType.Execute);

            foreach (var action in actions)
            {
                await ProcessExecuteActionAsync(action, rootLocation);
            }
        }

        private Task ProcessInstallActionAsync(ComponentAction action, string rootLocation)
        {
            throw new NotImplementedException();
        }

        private Task ProcessExecuteActionAsync(ComponentAction action, string rootLocation)
        {
            throw new NotImplementedException();
        }
    }
}
