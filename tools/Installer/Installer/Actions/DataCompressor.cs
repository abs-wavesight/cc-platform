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

        public async Task CompressDirectoryAsync(DirectoryInfo source, FileInfo destination)
        {
            await Task.Yield();
            File.Delete(destination.FullName);
            ZipFile.CreateFromDirectory(source.FullName, destination.FullName, CompressionLevel.SmallestSize, false);
        }

        public async Task DecompressFileAsync(FileInfo source, DirectoryInfo destination)
        {
            await Task.Yield();
            ZipFile.ExtractToDirectory(source.FullName, destination.FullName, true);
        }
    }
}
