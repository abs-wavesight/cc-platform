using System.ServiceProcess;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1416

namespace Abs.CommonCore.Installer.Actions;

public abstract class ActionBase
{
    protected static Component[] DetermineComponents(InstallerComponentRegistryConfig registryConfig, string[]? specificComponents, ICollection<string>? configComponents)
    {
        try
        {
            if (specificComponents?.Length > 0)
            {
                return specificComponents
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }

            if (configComponents?.Count > 0)
            {
                return configComponents
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to determine components to use", ex);
        }

        throw new Exception("No components found to use");
    }

    protected static void ReadMissingParameters(Dictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Value))
            {
                Console.Write($"Enter value for parameter '{parameter.Key}': ");
                parameters[parameter.Key] = Console.ReadLine() ?? "";
            }
        }
    }

    protected async Task RemoveWindowsServiceAsync(ILogger logger, ICommandExecutionService commandExecutionService, string name, bool retry)
    {
        logger.LogInformation($"Deleting service '{name}'");
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            logger.LogInformation($"Service '{name}' does not exist");
            return;
        }

        await StopWindowsServiceAsync(logger, commandExecutionService, name);
        await commandExecutionService.ExecuteCommandAsync("sc", $"delete {name}", "");
        await Task.Delay(1000);

        service = GetWindowsServiceByName(name);
        if (service != null && retry)
        {
            logger.LogInformation($"Service '{name}' still exists. Retrying removal.");
            await Task.Delay(5000);
            await RemoveWindowsServiceAsync(logger, commandExecutionService, name, false);
        }

        if (service == null)
        {
            logger.LogInformation($"Service '{name}' deleted");
        }
    }

    protected static async Task StopWindowsServiceAsync(ILogger logger, ICommandExecutionService commandExecutionService, string name)
    {
        logger.LogInformation($"Stopping service '{name}'");
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            logger.LogInformation($"Service '{name}' does not exist");
            return;
        }

        await commandExecutionService.ExecuteCommandAsync("net", $"stop {name}", "");
        await Task.Delay(1000);
        service?.Refresh();
        service?.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
    }

    protected static async Task StartWindowsServiceAsync(ILogger logger, ICommandExecutionService commandExecutionService, string name)
    {
        logger.LogInformation($"Starting service '{name}'");
        var service = GetWindowsServiceByName(name);

        if (service == null)
        {
            throw new Exception($"Service '{name}' does not exist");
        }

        await commandExecutionService.ExecuteCommandAsync("net", $"start {name}", "");
        await Task.Delay(1000);
        service?.Refresh();
        service?.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
    }

    protected static ServiceController? GetWindowsServiceByName(string name)
    {
        return System.ServiceProcess.ServiceController
            .GetServices()
            .FirstOrDefault(x => x.DisplayName == name);
    }

    protected void DeleteRecursiveDirectory(ILogger logger, string path)
    {
        logger.LogInformation($"Deleting directory '{path}' recursively");
        foreach (var directory in Directory.GetDirectories(path))
        {
            DeleteRecursiveDirectory(logger, directory);
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
            Directory.Delete(path, true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(path, true);
        }
    }
}
