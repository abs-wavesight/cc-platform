using Abs.CommonCore.Installer.Actions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests.Actions;

public class DataChunkerTests : TestsBase
{
    [Fact]
    public async Task DataChunked()
    {
        var fullData = BuildTestData(16 * 1024);

        var tempFile = Path.GetTempFileName();
        var tempFileName = Path.GetFileName(tempFile);
        var destination = Directory.CreateTempSubdirectory("chunk");
        var expectedFiles = new[]
        {
            Path.Combine(destination.FullName, $"{tempFileName}.part1"),
            Path.Combine(destination.FullName, $"{tempFileName}.part2"),
            Path.Combine(destination.FullName, $"{tempFileName}.part3"),
            Path.Combine(destination.FullName, $"{tempFileName}.part4"),
        };

        try
        {
            await File.WriteAllBytesAsync(tempFile, fullData);

            var chunker = new DataChunker(NullLoggerFactory.Instance);
            await chunker.ChunkFileAsync(new FileInfo(tempFile), destination, 4096, true);

            var writtenBytes = expectedFiles
                .SelectMany(File.ReadAllBytes);

            Assert.Equal(fullData, writtenBytes);
        }
        finally
        {
            foreach (var file in expectedFiles)
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
        var tempPath = Path.GetTempPath();
        var tempDirectory = new DirectoryInfo(Path.Combine(tempPath, Guid.NewGuid().ToString()));
        var tempFile = Path.Combine(tempDirectory.FullName, "testFile");

        Directory.CreateDirectory(tempDirectory.FullName);
        try
        {
            await File.WriteAllBytesAsync($"{tempFile}.part1", file1);
            await File.WriteAllBytesAsync($"{tempFile}.part2", file2);
            await File.WriteAllBytesAsync($"{tempFile}.part3", file3);
            await File.WriteAllBytesAsync($"{tempFile}.part4", file4);

            var chunker = new DataChunker(NullLoggerFactory.Instance);
            await chunker.UnchunkFileAsync(tempDirectory, new FileInfo(tempFile), false);

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
