using Abs.CommonCore.Installer.Actions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests.Actions;

public class DataCompressorTests : TestsBase
{
    [Fact]
    public async Task DataCompressed()
    {
        var tempDir = Directory.CreateTempSubdirectory("compressorTests");

        await File.WriteAllBytesAsync($"{tempDir}\\file1", BuildTestData(1024));
        await File.WriteAllBytesAsync($"{tempDir}\\file2", BuildTestData(1024));
        await File.WriteAllBytesAsync($"{tempDir}\\file3", BuildTestData(1024));
        await File.WriteAllBytesAsync($"{tempDir}\\file4", BuildTestData(1024));

        var tempFile = Path.GetTempFileName();
        var tempFileInfo = new FileInfo(tempFile);

        var compressor = new DataCompressor(NullLoggerFactory.Instance);
        await compressor.CompressDirectoryAsync(tempDir, tempFileInfo, true);

        Assert.True(tempFileInfo.Exists);
        Assert.True(tempFileInfo.Length > 0);
    }

    [Fact]
    public async Task DataUncompressed()
    {
        var compressTempDir = Directory.CreateTempSubdirectory("compressorTests");

        var testData = Enumerable.Range(0, 4)
            .Select(x => BuildTestData(1024))
            .ToArray();

        await File.WriteAllBytesAsync($"{compressTempDir}\\file1", testData[0]);
        await File.WriteAllBytesAsync($"{compressTempDir}\\file2", testData[1]);
        await File.WriteAllBytesAsync($"{compressTempDir}\\file3", testData[2]);
        await File.WriteAllBytesAsync($"{compressTempDir}\\file4", testData[3]);

        var compressTempFile = Path.GetTempFileName();
        var compressTempFileInfo = new FileInfo(compressTempFile);

        // Not sure how else to verify
        var compressor = new DataCompressor(NullLoggerFactory.Instance);
        await compressor.CompressDirectoryAsync(compressTempDir, compressTempFileInfo, true);

        var uncompressTempDir = Directory.CreateTempSubdirectory("compressorTests");
        await compressor.UncompressFileAsync(compressTempFileInfo, uncompressTempDir, true);

        var newFile1 = await File.ReadAllBytesAsync($"{uncompressTempDir}\\file1");
        var newFile2 = await File.ReadAllBytesAsync($"{uncompressTempDir}\\file2");
        var newFile3 = await File.ReadAllBytesAsync($"{uncompressTempDir}\\file3");
        var newFile4 = await File.ReadAllBytesAsync($"{uncompressTempDir}\\file4");

        Assert.Equal(testData[0], newFile1);
        Assert.Equal(testData[1], newFile2);
        Assert.Equal(testData[2], newFile3);
        Assert.Equal(testData[3], newFile4);
    }
}
