using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Actions.Chunker
{
    public class ChunkerService
    {
        private readonly ILogger _logger;

        public ChunkerService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ChunkerService>();
        }

        public async Task ChunkFile(FileInfo source, DirectoryInfo destination, int maxSize)
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

        public async Task UnchunkFile(DirectoryInfo source, FileInfo destination)
        {
            if (source.Exists == false)
                throw new Exception("Source location doesn't exist");

            var files = Directory.GetFiles(source.FullName, "*.part?")
                .OrderBy(x =>
                {
                    var extension = Path.GetExtension(x).Replace("part", "");
                    return int.Parse(extension);
                });

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
