using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions
{
    public class DataChunker
    {
        private readonly ILogger _logger;

        public DataChunker(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataChunker>();
        }

        public async Task ChunkFileAsync(FileInfo source, DirectoryInfo destination, int maxSize)
        {
            if (source.Exists == false)
                throw new Exception("Source file does not exist");

            if (source.Length < maxSize)
                return;

            Directory.CreateDirectory(destination.FullName);

            _logger.LogInformation($"Breaking {source.Name} file into chunks");
            var bufferSize = Math.Min(4 * 1024, maxSize);
            var buffer = new byte[bufferSize];
            var currentChunkCount = 1;
            var chunkPath = Path.Combine(destination.FullName, source.Name);
            var currentChunkFile = File.OpenWrite($"{chunkPath}.part{currentChunkCount}");

            using (var stream = source.OpenRead())
            {
                var dataRead = int.MaxValue;
                var dataWritten = 0;

                while (dataRead > 0)
                {
                    dataRead = await stream.ReadAsync(buffer);

                    if (dataRead == 0)
                        break;

                    currentChunkFile ??= File.OpenWrite($"{chunkPath}.part{currentChunkCount}");
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
        }

        public async Task UnchunkFileAsync(DirectoryInfo source, FileInfo destination)
        {
            if (source.Exists == false)
                throw new Exception("Source location doesn't exist");

            var files = Directory.GetFiles(source.FullName, "*.part?")
                .OrderBy(x =>
                {
                    var extension = Path.GetExtension(x).Replace(".part", "");
                    return int.Parse(extension);
                })
                .ToArray();

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
        }
    }
}
