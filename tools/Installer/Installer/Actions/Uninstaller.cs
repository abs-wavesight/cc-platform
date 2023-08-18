using System.ComponentModel;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

// This call site is reachable on all platforms. 'ServiceController' is only supported on: 'windows'.
#pragma warning disable CA1416

namespace Abs.CommonCore.Installer.Actions
{
    public class Uninstaller : ActionBase
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
                var isABSContainer = container.Image
                    .Contains(AbsImageName, StringComparison.OrdinalIgnoreCase) ||
                        container.Labels.Any(x => x.Key.Contains(AbsContainerLabel, StringComparison.OrdinalIgnoreCase));

                if (isABSContainer == false)
                {
                    continue;
                }

                Console.WriteLine($"Removing container: {container.Names.StringJoin(", ")}");
                await client.Containers
                    .StopContainerAsync(container.ID, new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 10
                    });
            }

            Console.WriteLine("Pruning unused components");
            await _commandExecutionService.ExecuteCommandAsync("docker", "system prune -a -f", "");
            Console.WriteLine("System components removed");
        }

        private async Task UninstallConfigurationAsync(DirectoryInfo installPath)
        {
            await Task.Yield();

            Console.WriteLine("Removing configuration file");

            var configLocation = Path.Combine(installPath.FullName, "config");
            DeleteRecursiveDirectory(_logger, configLocation);

            Console.WriteLine("Configuration files removed");
        }

        private async Task UninstallDockerAsync(DirectoryInfo dockerLocation)
        {
            Console.WriteLine("Removing docker");

            await RemoveWindowsServiceAsync(_logger, _commandExecutionService, "dockerd", true);
            DeleteRecursiveDirectory(_logger, dockerLocation.FullName);
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
    }
}

