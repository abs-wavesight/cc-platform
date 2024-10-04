using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;

namespace Abs.CommonCore.Installer.Services;

#pragma warning disable CA1416
[ExcludeFromCodeCoverage]
public class WindowsServiceManager : IServiceManager
{
    private readonly ILogger _logger;
    private readonly ICommandExecutionService _commandExecutionService;

    public WindowsServiceManager(ILogger logger,
                                 ICommandExecutionService commandExecutionService)
    {
        _logger = logger;
        _commandExecutionService = commandExecutionService;
    }

    public async Task StopServiceAsync(string name)
    {
        _logger.LogInformation("Stopping service '{Name}'", name);
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            _logger.LogInformation("Service '{Name}' does not exist", name);
            return;
        }

        if (service.Status == ServiceControllerStatus.Stopped)
        {
            _logger.LogInformation("Service '{Name}' is stopped", name);
            return;
        }

        await _commandExecutionService.ExecuteCommandAsync("net", $"stop {name}", "");
        await Task.Delay(1000);
        service.Refresh();
        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
    }

    public async Task StartServiceAsync(string name)
    {
        _logger.LogInformation("Starting service '{Name}'", name);
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            throw new Exception($"Service '{name}' does not exist");
        }

        if (service.Status == ServiceControllerStatus.Running)
        {
            _logger.LogInformation("Service '{Name}' is already running", name);
            return;
        }

        await _commandExecutionService.ExecuteCommandAsync("net", $"start {name}", "");
        await Task.Delay(1000);
        service.Refresh();
        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
    }

    public async Task DeleteServiceAsync(string name, bool retry)
    {
        _logger.LogInformation("Deleting service '{Name}'", name);
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            _logger.LogInformation("Service '{Name}' does not exist", name);
            return;
        }

        await StopServiceAsync(name);
        await _commandExecutionService.ExecuteCommandAsync("sc", $"delete {name}", "");
        await Task.Delay(1000);

        service = GetWindowsServiceByName(name);
        if (service != null && retry)
        {
            _logger.LogInformation("Service '{Name}' still exists. Retrying removal.", name);
            await Task.Delay(5000);
            await DeleteServiceAsync(name, false);
        }

        if (service == null)
        {
            _logger.LogInformation("Service '{Name}' deleted", name);
        }
    }

    private static ServiceController? GetWindowsServiceByName(string name)
    {
        return ServiceController
                     .GetServices()
                     .FirstOrDefault(x => string.Equals(x.ServiceName, name, StringComparison.OrdinalIgnoreCase));
    }
}
