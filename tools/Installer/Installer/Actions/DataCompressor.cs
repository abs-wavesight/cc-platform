using System.IO.Compression;

namespace Abs.CommonCore.Installer.Actions;

public class DataCompressor : ActionBase
{
    private readonly ILogger _logger;
    private readonly CompressionLevel _compressionLevel = CompressionLevel.Optimal;

    public DataCompressor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DataCompressor>();
    }

    public async Task CompressDirectoryAsync(DirectoryInfo source, FileInfo destination, bool removeSource)
    {
        _logger.LogInformation($"Compressing folder '{source.FullName}' to file '{destination.FullName}'");

        if (Directory.Exists(source.FullName) == false)
        {
            _logger.LogWarning($"Source location '{source.FullName}' does not exist");
            return;
        }

        await Task.Yield();
        File.Delete(destination.FullName);
        ZipFile.CreateFromDirectory(source.FullName, destination.FullName, _compressionLevel, false);

        if (removeSource)
        {
            _logger.LogInformation($"Removing source folder: '{source.FullName}'");
            source.Delete(true);
        }
    }

    public async Task UncompressFileAsync(FileInfo source, DirectoryInfo destination, bool removeSource)
    {
        try
        {
            _logger.LogInformation($"Uncompressing file '{source.FullName}' to folder '{destination.FullName}'");

            if (File.Exists(source.FullName) == false)
            {
                _logger.LogWarning($"Source location '{source.FullName}' does not exist");
                return;
            }

            await Task.Yield();
            ZipFile.ExtractToDirectory(source.FullName, destination.FullName, true);

            if (removeSource)
            {
                _logger.LogInformation($"Removing source file: '{source.FullName}'");
                source.Delete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uncompressing file '{source.FullName}' to folder '{destination.FullName}'");
        }
    }
}
