using System.ServiceProcess;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public abstract class ActionBase
    {
        protected Component[] DetermineComponents(InstallerComponentRegistryConfig registryConfig, string[]? specificComponents, ICollection<string>? configComponents)
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

        protected void ReadMissingParameters(Dictionary<string, string> parameters)
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
            await StopWindowsServiceAsync(logger, commandExecutionService, name);

            logger.LogInformation($"Deleting service '{name}'");
            await commandExecutionService.ExecuteCommandAsync("sc", $"delete {name}", "");
            await Task.Delay(1000);

            var service = GetWindowsServiceByName(name);
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

        protected async Task StopWindowsServiceAsync(ILogger logger, ICommandExecutionService commandExecutionService, string name)
        {
            var service = GetWindowsServiceByName(name);

            logger.LogInformation($"Stopping service '{name}'");
            await commandExecutionService.ExecuteCommandAsync("net", $"stop {name}", "");
            await Task.Delay(1000);
            service?.Refresh();
            service?.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        }

        protected ServiceController? GetWindowsServiceByName(string name)
        {
            return System.ServiceProcess.ServiceController
                .GetServices()
                .FirstOrDefault(x => x.DisplayName == name);
        }

        protected void DeleteRecursiveDirectory(ILogger logger, string path)
        {
            logger.LogInformation($"Deleting directory '{path}' recursively");
            foreach (string directory in Directory.GetDirectories(path))
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
}
