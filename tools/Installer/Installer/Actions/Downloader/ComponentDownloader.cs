using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using Abs.CommonCore.Installer.Actions.Downloader.Config;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging;
using Component = Abs.CommonCore.Installer.Actions.Downloader.Config.Component;

namespace Abs.CommonCore.Installer.Actions.Downloader
{
    public class ComponentDownloader
    {
        private readonly ILogger _logger;
        private readonly DownloaderConfig _config;

        public ComponentDownloader(ILoggerFactory loggerFactory, FileInfo registry)
        {
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();
            _config = ConfigParser.LoadConfig<DownloaderConfig>(registry.FullName);
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting downloader");

            Directory.CreateDirectory(_config.OutputLocation);

            foreach (var component in _config.Components)
            {
                _logger.LogInformation($"Starting component '{component.Name}'");

                try
                {
                    await ExecuteComponentAsync(component);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to execute component '{component.Name}'", ex);
                }
            }

            _logger.LogInformation("Downloader complete");
        }

        private Task ExecuteComponentAsync(Component component)
        {
            if (component.Type == ComponentType.Docker) return ExecuteDockerComponentAsync(component);
            throw new Exception($"Unknown component type '{component.Type}'");
        }

        private async Task ExecuteDockerComponentAsync(Component component)
        {
            foreach (var file in component.Files)
            {
                await ProcessFileAsync(component, file.Type, file.Source, file.Destination);
            }
        }

        private Task ProcessFileAsync(Component component, FileType fileType, string source, string destination)
        {
            if (fileType == FileType.Container) return ProcessContainerFileAsync(component, source, destination);
            throw new Exception($"Unknown file type '{fileType}'");
        }

        private async Task ProcessContainerFileAsync(Component component, string source, string destination)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            var containerFile = Path.Combine(rootLocation, destination);
            Directory.CreateDirectory(rootLocation);

            _logger.LogInformation($"Pulling image: {source}");
            await ExecuteCommandAsync("docker", $"pull {source}");

            _logger.LogInformation($"Saving image: {source}");
            await ExecuteCommandAsync("docker", $"save -o {containerFile} {source}");
        }

        private async Task ExecuteCommandAsync(string command, string arguments)
        {
            _logger.LogInformation($"Executing: {command} {arguments}");

            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.ErrorDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data) == false) _logger.LogError(args.Data);
            };
            process.OutputDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data) == false) _logger.LogInformation(args.Data);
            };
            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync();
        }
    }
}
