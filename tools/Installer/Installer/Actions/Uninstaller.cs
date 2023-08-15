using System.ServiceProcess;
using Abs.CommonCore.Installer.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class Uninstaller
    {
        private const string AbsContainerLabel = "org.eagle.wavesight";
        private const string AbsImageName = "abs-wavesight";

        private readonly ILogger _logger;
        private readonly ICommandExecutionService _commandExecutionService;

        public Uninstaller(ILoggerFactory loggerFactory, ICommandExecutionService commandExecutionService)
        {
            _logger = loggerFactory.CreateLogger<Uninstaller>();
            _commandExecutionService = commandExecutionService;
        }

        public async Task UninstallSystemAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath, bool? removeSystem, bool? removeConfig, bool? removeDocker)
        {
            Console.WriteLine("Starting uninstallation process");
            await UninstallSystemComponentsAsync(dockerLocation, installPath, removeSystem, removeConfig, removeDocker);
        }

        private async Task UninstallSystemComponentsAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath, bool? removeSystem, bool? removeConfig, bool? removeDocker)
        {
            // Null used by unit tests to skip check - False is default command line value since bool? seems not supported
            var removeComponents = removeSystem != null && (removeSystem.Value || ReadBoolean("Remove system components"));
            var removeConfiguration = removeConfig != null && (removeConfig.Value || ReadBoolean("Remove configuration files"));
            var removeDockerItems = removeDocker != null && (removeDocker.Value || ReadBoolean("Remove docker"));

            if (removeConfiguration && (installPath == null || installPath.Exists == false))
            {
                installPath = ReadDirectoryPath("Enter installation location");
            }

            if (removeDockerItems && (dockerLocation == null || dockerLocation.Exists == false))
            {
                dockerLocation = ReadDirectoryPath("Enter docker location");
            }

            if (removeComponents) await UninstallSystemComponentsAsync();
            if (removeConfiguration) await UninstallConfigurationAsync(installPath!);
            if (removeDockerItems) await UninstallDockerAsync(dockerLocation!);
        }

        private async Task UninstallSystemComponentsAsync()
        {
            Console.WriteLine("Removing system components");
            var client = new DockerClientConfiguration()
                .CreateClient();

            var allContainers = await client.Containers
                .ListContainersAsync(new ContainersListParameters { All = true });

            foreach (var container in allContainers)
            {
                var isABSContainer = container.Image.Contains(AbsImageName, StringComparison.OrdinalIgnoreCase) ||
                                     container.Labels
                    .Any(x => x.Key.Contains(AbsContainerLabel, StringComparison.OrdinalIgnoreCase));

                if (isABSContainer == false)
                {
                    continue;
                }

                Console.WriteLine($"Removing container: {container.Image}");
                await client.Containers
                    .StopContainerAsync(container.ID, new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 5
                    });

                await client.Containers
                    .RemoveContainerAsync(container.ID, new ContainerRemoveParameters
                    {
                        RemoveLinks = false,
                        RemoveVolumes = true,
                        Force = true
                    });
            }

            Console.WriteLine("System components removed");
        }

        private async Task UninstallConfigurationAsync(DirectoryInfo installPath)
        {
            await Task.Yield();

            Console.WriteLine("Removing configuration file");

            var configLocation = Path.Combine(installPath.FullName, "config");
            DeleteRecursiveDirectory(configLocation);

            Console.WriteLine("Configuration files removed");
        }

        private async Task UninstallDockerAsync(DirectoryInfo dockerLocation)
        {
            Console.WriteLine("Removing docker");

            await RemoveWindowsServiceAsync("dockerd", true);
            DeleteRecursiveDirectory(dockerLocation.FullName);
            var path = Environment.GetEnvironmentVariable(Constants.PathEnvironmentVariable)!;

            if (path.Contains(dockerLocation.FullName, StringComparison.OrdinalIgnoreCase))
            {
                path = path
                    .Replace(dockerLocation.FullName, "", StringComparison.OrdinalIgnoreCase);

                await _commandExecutionService.ExecuteCommandAsync("setx", $"/M {Constants.PathEnvironmentVariable} \"{path}\"", "");
            }

            Console.WriteLine("Docker removed");
        }

        private DirectoryInfo ReadDirectoryPath(string prompt)
        {
            prompt = prompt
                .Trim(new[] { ' ', ':' });

            while (true)
            {
                Console.Write($"{prompt}: ");
                var text = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(text) || Directory.Exists(text) == false)
                {
                    Console.WriteLine("Unable to access location");
                    continue;
                }

                return new DirectoryInfo(text);
            }
        }

        private bool ReadBoolean(string prompt)
        {
            prompt = prompt
                .Trim(new[] { ' ', ':' });

            while (true)
            {
                Console.Write($"{prompt}? (y/n): ");
                var text = Console.ReadLine();

                var result = ConvertToBoolean(text);
                if (result == null)
                {
                    Console.WriteLine("Unable to read response");
                    continue;
                }

                return result.Value;
            }
        }

        private bool? ConvertToBoolean(string? text)
        {
            text = (text ?? "")
                .Trim()
                .ToLower();

            if (string.IsNullOrWhiteSpace(text)) return null;

            var isSuccess = bool.TryParse(text, out var result);
            if (isSuccess) return result;

            if (text == "y") return true;
            if (text == "n") return false;

            return null;
        }

        private async Task RemoveWindowsServiceAsync(string name, bool retry)
        {
            var service = GetWindowsServiceByName(name);

            if (service == null)
            {
                _logger.LogInformation($"Service '{name}' does not exist");
                return;
            }

            await _commandExecutionService.ExecuteCommandAsync("net", $"stop {name}", "");
            await Task.Delay(1000);
            service.Refresh();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

            await _commandExecutionService.ExecuteCommandAsync("sc", $"delete {name}", "");
            await Task.Delay(1000);

            service = GetWindowsServiceByName(name);
            if (service != null && retry)
            {
                _logger.LogInformation($"Service '{name}' still exists. Retrying removal.");
                await Task.Delay(5000);
                await RemoveWindowsServiceAsync(name, false);
            }

            if (service == null)
            {
                _logger.LogInformation($"Service '{name}' deleted");
            }
        }

        private ServiceController? GetWindowsServiceByName(string name)
        {
            return System.ServiceProcess.ServiceController
                .GetServices()
                .FirstOrDefault(x => x.DisplayName == name);
        }

        private void DeleteRecursiveDirectory(string path)
        {
            _logger.LogInformation($"Deleting directory '{path}' recursively");
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteRecursiveDirectory(directory);
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

