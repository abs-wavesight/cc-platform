using Abs.CommonCore.Installer.Actions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests
{
    public class DataChunkerTests : TestsBase
    {
        [Fact]
        public async Task DataChunked()
        {
            var fullData = BuildTestData(16 * 1024);

            var tempFile = Path.GetTempFileName();
            var expectedFiles = new[]
            {
                $"{tempFile}.part1", $"{tempFile}.part2", $"{tempFile}.part3", $"{tempFile}.part4",
            };

            try
            {
                await File.WriteAllBytesAsync(tempFile, fullData);

                var destination = new DirectoryInfo(Path.GetTempPath());

                var chunker = new DataChunker(NullLoggerFactory.Instance);
                await chunker.ChunkFileAsync(new FileInfo(tempFile), destination, 1024);

                var writtenBytes = expectedFiles
                    .SelectMany(x => File.ReadAllBytes(x));

                Assert.Equal(fullData, writtenBytes);
            }
            finally
            {
                var allFiles = expectedFiles
                    .Concat(new[] { tempFile });

                foreach (var file in allFiles)
                {
                    File.Delete(file);
                }

            }
        }

        [Fact]
        public async Task DataUnchunked()
        {
            var file1 = BuildTestData(1024);
            var file2 = BuildTestData(1024);
            var file3 = BuildTestData(1024);
            var file4 = BuildTestData(1024);

            var expectedData = file1.Concat(file2).Concat(file3).Concat(file4);
            var tempFile = Path.GetTempFileName();
            var tempDirectory = new DirectoryInfo(Path.GetTempPath());

            try
            {
                var files = Directory.GetFiles(tempDirectory.FullName, "*.part?");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                await File.WriteAllBytesAsync($"{tempFile}.part1", file1);
                await File.WriteAllBytesAsync($"{tempFile}.part2", file2);
                await File.WriteAllBytesAsync($"{tempFile}.part3", file3);
                await File.WriteAllBytesAsync($"{tempFile}.part4", file4);

                var chunker = new DataChunker(NullLoggerFactory.Instance);
                await chunker.UnchunkFileAsync(tempDirectory, new FileInfo(tempFile));

                var writtenData = await File.ReadAllBytesAsync(tempFile);
                Assert.Equal(expectedData, writtenData);
            }
            finally
            {
                File.Delete(tempFile);
                File.Delete($"{tempFile}.part1");
                File.Delete($"{tempFile}.part2");
                File.Delete($"{tempFile}.part3");
                File.Delete($"{tempFile}.part4");
            }
        }
    }
}
