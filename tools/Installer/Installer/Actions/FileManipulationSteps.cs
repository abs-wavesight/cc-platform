using System.Text.Json;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Extensions;

namespace Abs.CommonCore.Installer.Actions;
public class FileManipulationSteps(ILogger logger,
    ILoggerFactory loggerFactory,
    ICommandExecutionService commandExecutionService,
    InstallerComponentRegistryConfig registryConfig)
{
    private const int DefaultMaxChunkSize = 1 * 1024 * 1024 * 1024; // 1GB
    private const string ReleaseZipName = "Release.zip";
    private const string AdditionalFilesName = "AdditionalFiles";

    public void VerifySourcesPresent(Component[] components)
    {
        logger.LogInformation("Verifying source files are present");
        var requiredFiles = components
            .SelectMany(component => component.Actions, (component, action) => new { component, action })
            .Where(t => t.action.Action is ComponentActionAction.Install or ComponentActionAction.Copy)
            .Where(t => !t.action.AdditionalProperties.TryGetValue("skipValidation", out var val) || ((JsonElement)val).GetBoolean() != true)
            .Select(t => Path.Combine(registryConfig.Location, t.component.Name, t.action.Source))
            .Select(Path.GetFullPath)
            .ToArray();
        logger.LogInformation($"Required installation files: {requiredFiles.StringJoin("; ")}");

        var missingFiles = requiredFiles
            .Where(location => VerifyFileExists(location) == false)
            .ToArray();

        if (missingFiles.Any())
        {
            throw new Exception($@"Required installation files are missing: {missingFiles.StringJoin("; " + Environment.NewLine)}");
        }
    }

    private static bool VerifyFileExists(string location)
    {
        var directory = Path.GetDirectoryName(location)!;
        var filename = Path.GetFileName(location);

        if (filename.Contains('*') || filename.Contains('?'))
        {
            var files = Directory.GetFiles(directory, filename);
            return files.Length > 0;
        }

        return File.Exists(location);
    }

    public async Task RunUpdatePathCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        logger.LogInformation($"{component.Name}: Adding '{action.Source}' to system path");
        var path = Environment.GetEnvironmentVariable(Constants.PathEnvironmentVariable, EnvironmentVariableTarget.Machine)
                   ?? "";

        if (path.Contains(action.Source, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await commandExecutionService.ExecuteCommandAsync("setx", $"/M {Constants.PathEnvironmentVariable} \"%{Constants.PathEnvironmentVariable}%;{action.Source}\"", rootLocation);
    }

    public async Task RunCopyCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        logger.LogInformation($"{component.Name}: Copying file '{action.Source}' to '{action.Destination}'");
        var directory = Path.GetDirectoryName(action.Destination)!;

        if (string.IsNullOrWhiteSpace(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        await commandExecutionService.ExecuteCommandAsync("copy", $"\"{action.Source}\" \"{action.Destination}\"", rootLocation);
    }

    public async Task RunReplaceParametersCommandAsync(Component component, string rootLocation, ComponentAction action, Dictionary<string, string> allParameters)
    {
        logger.LogInformation($"{component.Name}: Replacing parameters in '{action.Source}'");
        var path = Path.Combine(rootLocation, action.Source);
        var text = await File.ReadAllTextAsync(path);

        foreach (var param in allParameters)
        {
            text = text.Replace(param.Key, param.Value, StringComparison.OrdinalIgnoreCase);
        }

        await File.WriteAllTextAsync(path, text);
    }

    public async Task RunChunkCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var chunker = new DataChunker(loggerFactory);

        var source = new FileInfo(Path.Combine(rootLocation, action.Source));
        var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
        await chunker.ChunkFileAsync(source, destination, DefaultMaxChunkSize, false);
    }

    public async Task RunUnchunkCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var chunker = new DataChunker(loggerFactory);

        var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
        var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
        await chunker.UnchunkFileAsync(source, destination, false);
    }

    public async Task RunCompressCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var compressor = new DataCompressor(loggerFactory);

        var source = new DirectoryInfo(Path.Combine(rootLocation, action.Source));
        var destination = new FileInfo(Path.Combine(rootLocation, action.Destination));
        await compressor.CompressDirectoryAsync(source, destination, false);
    }

    public async Task RunUncompressCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        var compressor = new DataCompressor(loggerFactory);

        var source = new FileInfo(Path.Combine(rootLocation, action.Source));
        var destination = new DirectoryInfo(Path.Combine(rootLocation, action.Destination));
        await compressor.UncompressFileAsync(source, destination, false);
    }

    public async Task<string[]> PrintReadmeFileAsync()
    {
        var current = Directory.GetCurrentDirectory();
        var readmeName = Directory.GetFiles(current, "readme*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (string.IsNullOrEmpty(readmeName))
        {
            return new string[0];
        }

        var readmePath = Path.Combine(current, readmeName);
        var readmeExists = File.Exists(readmePath);

        var readmeLines = await File.ReadAllLinesAsync(readmePath);

        foreach (var line in readmeLines)
        {
            logger.LogInformation(line);
        }

        return readmeLines;
    }

    public async Task ExpandReleaseZipFile()
    {
        logger.LogInformation("Preparing install components");

        var current = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(current, "*.zip*", SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            logger.LogInformation("No release files found");
            return;
        }

        var releaseZip = new FileInfo(Path.Combine(current, ReleaseZipName));
        var installLocation = new DirectoryInfo(registryConfig.Location);

        if (!releaseZip.Exists)
        {
            logger.LogInformation("Unchunking release files");
            var chunker = new DataChunker(loggerFactory);
            await chunker.UnchunkFileAsync(new DirectoryInfo(current), releaseZip, false);
            if (!releaseZip.Exists)
            {
                var release = Directory.GetFiles(current, "*.zip", SearchOption.TopDirectoryOnly);
                if (release.Length == 1)
                {
                    releaseZip = new FileInfo(release[0]);
                    File.Copy(releaseZip.FullName, Path.Combine(current, ReleaseZipName));
                }
            }
        }
        else
        {
            logger.LogInformation("Skip unchunking release files");
        }

        logger.LogInformation("Uncompressing release file");
        var compressor = new DataCompressor(loggerFactory);
        await compressor.UncompressFileAsync(releaseZip, installLocation, false);

        logger.LogInformation($"Creating {AdditionalFilesName} folder");
        var path = Path.Combine(registryConfig.Location, AdditionalFilesName);
        Directory.CreateDirectory(path);
    }
}
