using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions.Compression
{
    public class DataCompressor
    {
        private readonly ILogger _logger;

        public DataCompressor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataCompressor>();
        }

        public async Task CompressDirectory(DirectoryInfo source, FileInfo destination)
        {
            await Task.Yield();
            File.Delete(destination.FullName);
            ZipFile.CreateFromDirectory(source.FullName, destination.FullName, CompressionLevel.Optimal, false);
        }

        public async Task DecompressFile(FileInfo source, DirectoryInfo destination)
        {
            await Task.Yield();
            ZipFile.ExtractToDirectory(source.FullName, destination.FullName, true);
        }
    }
}
