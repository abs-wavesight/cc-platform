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

        public async Task UninstallSystemAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath)
        {
            Console.WriteLine("Starting uninstallation process");
            await UninstallSystemComponentsAsync(dockerLocation, installPath);
        }

        private async Task UninstallSystemComponentsAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath)
        {
            var removeComponents = ReadBoolean("Remove system components");
            if (removeComponents) await UninstallSystemComponentsAsync();

            var removeConfiguration = ReadBoolean("Remove configuration files");
            if (removeConfiguration) await UninstallConfigurationAsync(installPath);

            var removeDocker = ReadBoolean("Remove docker");
            if (removeDocker) await UninstallDockerAsync(dockerLocation);
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

        private async Task UninstallConfigurationAsync(DirectoryInfo? installPath)
        {
            if (installPath == null || installPath.Exists == false)
            {
                installPath = ReadDirectoryPath("Enter installation location");
            }

            Console.WriteLine("Removing configuration file");

            var configLocation = Path.Combine(installPath.FullName, "config");
            Directory.Delete(configLocation, true);

            Console.WriteLine("Configuration files removed");
        }

        private async Task UninstallDockerAsync(DirectoryInfo? dockerLocation)
        {
            if (dockerLocation == null || dockerLocation.Exists == false)
            {
                dockerLocation = ReadDirectoryPath("Enter docker location");
            }

            Console.WriteLine("Removing docker");

            await _commandExecutionService.ExecuteCommandAsync("net", "stop dockerd", "");
            Directory.Delete(dockerLocation.FullName, true);
            await _commandExecutionService.ExecuteCommandAsync("sc", "delete dockerd", "");

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

