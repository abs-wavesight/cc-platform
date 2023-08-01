using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class DataCompressor
    {
        private readonly ILogger _logger;

        public DataCompressor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataCompressor>();
        }

        public async Task CompressDirectoryAsync(DirectoryInfo source, FileInfo destination, bool removeSource)
        {
            _logger.LogInformation($"Compressing folder '{source.FullName}' to file '{destination.FullName}'");

            if (Directory.Exists(source.FullName) == false)
                throw new Exception("Source location does not exist");

            await Task.Yield();
            File.Delete(destination.FullName);
            ZipFile.CreateFromDirectory(source.FullName, destination.FullName, CompressionLevel.SmallestSize, false);

            if (removeSource)
            {
                _logger.LogInformation($"Removing source folder: '{source.FullName}'");
                source.Delete(true);
            }
        }

        public async Task UncompressFileAsync(FileInfo source, DirectoryInfo destination, bool removeSource)
        {
            _logger.LogInformation($"Uncompressing file '{source.FullName}' to folder '{destination.FullName}'");

            if (File.Exists(source.FullName) == false)
                throw new Exception("Source location does not exist");

            await Task.Yield();
            ZipFile.ExtractToDirectory(source.FullName, destination.FullName, true);

            if (removeSource)
            {
                _logger.LogInformation($"Removing source file: '{source.FullName}'");
                source.Delete();
            }
        }
    }
}
