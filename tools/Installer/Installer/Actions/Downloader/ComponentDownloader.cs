using System.Diagnostics;
using System.Net.Http.Headers;
using Abs.CommonCore.Installer.Actions.Downloader.Config;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Logging;
using Component = Abs.CommonCore.Installer.Actions.Downloader.Config.Component;

namespace Abs.CommonCore.Installer.Actions.Downloader
{
    public class ComponentDownloader : IDisposable
    {
        private const string _nugetEnvironmentVariableName = "ABS_NUGET_PASSWORD";

        private readonly ILogger _logger;
        private readonly DownloaderConfig _config;

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string? _nugetEnvironmentVariable;

        public ComponentDownloader(ILoggerFactory loggerFactory, FileInfo registry)
        {
            _logger = loggerFactory.CreateLogger<ComponentDownloader>();
            _config = ConfigParser.LoadConfig<DownloaderConfig>(registry.FullName);

            _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(_nugetEnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(_nugetEnvironmentVariable))
            {
                throw new Exception("Nuget environment variable not found");
            }
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

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private Task ExecuteComponentAsync(Component component)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            Directory.CreateDirectory(rootLocation);

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
            if (fileType == FileType.File) return ProcessSimpleFileAsync(component, source, destination);
            throw new Exception($"Unknown file type '{fileType}'");
        }

        private async Task ProcessContainerFileAsync(Component component, string source, string destination)
        {
            var rootLocation = Path.Combine(_config.OutputLocation, component.Name);
            var containerFile = Path.Combine(rootLocation, destination);

            _logger.LogInformation($"Pulling image '{source}'");
            await ExecuteCommandAsync("docker", $"pull {source}");

            _logger.LogInformation($"Saving image '{source}' to '{destination}'");
            await ExecuteCommandAsync("docker", $"save -o {containerFile} {source}");
        }

        private async Task ProcessSimpleFileAsync(Component component, string source, string destination)
        {
            var outputPath = Path.Combine(_config.OutputLocation, component.Name, destination);

            _logger.LogInformation($"Downloading file '{source}'");
            var data = await DownloadDataAsync(source);

            _logger.LogInformation($"Saving file '{source}' to '{destination}'");
            await File.WriteAllBytesAsync(outputPath, data);
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

        private async Task<byte[]> DownloadDataAsync(string source)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, source))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nugetEnvironmentVariable);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));

                using (var response = await _httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }

            return Array.Empty<byte>();
        }
    }
}
