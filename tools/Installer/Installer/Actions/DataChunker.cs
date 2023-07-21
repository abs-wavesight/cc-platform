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

            Directory.CreateDirectory(destination.Name);

            _logger.LogInformation($"Breaking {source.Name} file into chunks");
            var buffer = new byte[4 * 1024];
            var currentChunkCount = 1;
            var currentChunkFile = File.OpenWrite($"{source.FullName}.part{currentChunkCount}");

            using (var stream = source.OpenRead())
            {
                var dataRead = int.MaxValue;
                var dataWritten = 0;

                while (dataRead > 0)
                {
                    dataRead = await stream.ReadAsync(buffer);
                    await currentChunkFile.WriteAsync(buffer, 0, dataRead);
                    dataWritten += dataRead;

                    if (dataWritten > maxSize)
                    {
                        currentChunkFile.Close();
                        dataWritten = 0;
                        currentChunkCount++;
                        currentChunkFile = File.OpenWrite($"{source.FullName}.part{currentChunkCount}");
                    }
                }
            }

            currentChunkFile.Close();
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
