using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class DataChunker
    {
        private readonly ILogger _logger;
        private const string ChunkName = "part";

        public DataChunker(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataChunker>();
        }

        public async Task ChunkFileAsync(FileInfo source, DirectoryInfo destination, int maxSize, bool removeSource)
        {
            _logger.LogInformation($"Chunking file '{source.FullName}' to folder '{destination.FullName}'");

            if (source.Exists == false)
                throw new Exception("Source file does not exist");

            if (source.Length < maxSize)
            {
                _logger.LogInformation("Source file is under max size");
                return;
            }

            Directory.CreateDirectory(destination.FullName);

            var bufferSize = Math.Min(4 * 1024, maxSize);
            var buffer = new byte[bufferSize];
            var currentChunkCount = 1;
            var chunkPath = Path.Combine(destination.FullName, source.Name);
            var currentChunkFile = File.OpenWrite($"{chunkPath}.{ChunkName}{currentChunkCount}");

            using (var stream = source.OpenRead())
            {
                var dataRead = int.MaxValue;
                var dataWritten = 0;

                while (dataRead > 0)
                {
                    dataRead = await stream.ReadAsync(buffer);

                    if (dataRead == 0)
                        break;

                    currentChunkFile ??= File.OpenWrite($"{chunkPath}.{ChunkName}{currentChunkCount}");
                    await currentChunkFile.WriteAsync(buffer, 0, dataRead);
                    dataWritten += dataRead;

                    if (dataWritten >= maxSize)
                    {
                        currentChunkFile.Close();
                        dataWritten = 0;
                        currentChunkCount++;
                        currentChunkFile = null;
                    }
                }
            }

            currentChunkFile?.Close();
            if (removeSource) source.Delete();
        }

        public async Task UnchunkFileAsync(DirectoryInfo source, FileInfo destination, bool removeSource)
        {
            _logger.LogInformation($"Unchunking folder '{source.FullName}' to file '{destination.FullName}'");

            if (source.Exists == false)
                throw new Exception("Source location doesn't exist");

            var files = Directory.GetFiles(source.FullName, $"*.{ChunkName}?")
                .OrderBy(x =>
                {
                    var extension = Path.GetExtension(x).Replace($".{ChunkName}", "");
                    return int.Parse(extension);
                })
                .ToArray();

            if (files.Length == 0)
            {
                return;
            }

            destination.Delete();
            using (var destinationStream = destination.OpenWrite())
            {
                foreach (var file in files)
                {
                    using (var fileStream = File.OpenRead(file))
                    {
                        await fileStream.CopyToAsync(destinationStream);
                    }
                }
            }

            if (removeSource) source.Delete(true);
        }
    }
}
