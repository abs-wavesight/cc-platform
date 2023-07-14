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

            foreach (var component in _config.Components)
            {
                _logger.LogInformation($"Starting component '{component.Name}'");

                try
                {
                    await ExecuteComponentAsync(component);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to install component '{component.Name}'", ex);
                }
            }

            _logger.LogInformation("Installer complete");
        }

        private async Task ExecuteComponentAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            Directory.CreateDirectory(rootLocation);

            foreach (var action in component.Actions)
            {
                await ProcessComponentActionAsync(action);
            }
        }

        private Task ProcessComponentActionAsync(ComponentAction action)
        {
            if (action.Action == ActionType.Install) return ProcessInstallActionAsync(action);
            if (action.Action == ActionType.Execute) return ProcessExecuteActionAsync(action);
            throw new Exception($"Unknown action type '{action.Action}'");
        }

        private Task ProcessInstallActionAsync(ComponentAction action)
        {
            throw new NotImplementedException();
        }

        private Task ProcessExecuteActionAsync(ComponentAction action)
        {
            throw new NotImplementedException();
        }
    }
}
