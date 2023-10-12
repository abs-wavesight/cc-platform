using Abs.CommonCore.Installer;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;

namespace Installer.Tests.Actions;

public class ComponentDownloaderTests
{
    [Fact]
    public void InvalidRegistryConfig_ThrowsException()
    {
        Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_RegistryConfig.json"));
    }

    [Fact]
    public void InvalidDownloaderConfig_ThrowsException()
    {
        Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_RegistryConfig.json", @"Configs/Invalid_DownloaderConfig.json"));
    }

    [Fact]
    public async Task InvalidRegistryConfigValues_ThrowsException()
    {
        var (_, _, Downloader) = Initialize(@"Configs/Invalid2_RegistryConfig.json");
        await Assert.ThrowsAsync<Exception>(() => Downloader.ExecuteAsync());
    }

    [Fact]
    public async Task InvalidDownloaderConfigValues_ThrowsException()
    {
        var (_, _, Downloader) = Initialize(@"Configs/RegistryConfig.json", @"Configs/InvalidComponent_DownloaderConfig.json");
        await Assert.ThrowsAsync<Exception>(() => Downloader.ExecuteAsync());
    }

    [Fact]
    public async Task ValidConfig_FileDownloaded()
    {
        var (DataRequest, _, Downloader) = Initialize(@"Configs/RegistryConfig.json");
        await Downloader.ExecuteAsync(new[] { "RabbitMq" });

        DataRequest.Verify(x => x.RequestByteArrayAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ParameterizedConfig_FileDownloaded()
    {
        var paramKey = "$SOME_DOWNLOAD_PARAM";
        var paramValue = "Replacement";

        var parameters = new Dictionary<string, string>() { { paramKey, paramValue } };
        var (DataRequest, _, Downloader) = Initialize(@"Configs/ParameterizedRegistryConfig.json", parameters: parameters);
        await Downloader.ExecuteAsync(new[] { "RabbitMq" });

        DataRequest.Verify(x => x.RequestByteArrayAsync(paramValue), Times.Exactly(1));
    }

    [Fact]
    public async Task ValidConfig_CommandExecuted()
    {
        var (_, CommandExecute, Downloader) = Initialize(@"Configs/RegistryConfig.json");
        await Downloader.ExecuteAsync(new[] { "RabbitMq" });

        CommandExecute.Verify(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(@"Configs/DownloadTest_RegistryConfig.json")]
    [InlineData(@"Configs/DownloadTest_GithubRawFileConfig.json")]
    [InlineData(@"Configs/DownloadTest_GithubReleaseFileConfig.json")]
    public async Task ValidConfig_RealFileDownloaded(string configPath)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var dataRequest = new DataRequestService(loggerFactory);
        var commandExecution = new CommandExecutionService(loggerFactory);
        var registry = new FileInfo(configPath);
        var config = new FileInfo(@"Configs/DownloaderConfig.json");
        var parameters = new Dictionary<string, string>();

        var expectedFile = @"c:\abs\installer\RabbitMq\download_file";
        File.Delete(expectedFile);

        var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, registry, config, parameters, false);
        await downloader.ExecuteAsync();

        Assert.True(File.Exists(expectedFile));
        File.Delete(expectedFile);
    }

    [Fact]
    public async Task DownloadLatest_Vector_Central_Release()
    {
        var configText = await File.ReadAllTextAsync(@"Configs/DownloadTest_LatestVectorCentral_Release.json");
        var tempFile = Path.GetTempFileName();
        var nugetEnvironmentVariable = Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);
        var expectedFilePath = Path.Combine(Path.GetTempPath(), "Test_Release_Download");
        var expectedFile = Path.Combine(expectedFilePath, "2019.zip");

        Directory.CreateDirectory(expectedFilePath);
        File.Delete(expectedFile);

        var client = new GitHubClient(new Octokit.ProductHeaderValue("ABS"))
        {
            Credentials = new Credentials(nugetEnvironmentVariable)
        };

        var releases = await client.Repository.Release.GetAll("abs-wavesight", "cc-platform");
        var vectorCentralRelease = releases.FirstOrDefault(x => x.TagName.StartsWith("vector-central"));

        if (vectorCentralRelease == null)
        {
            // No releases to test
            return;
        }

        configText = configText
            .Replace("$VECTOR_TAG", vectorCentralRelease!.TagName)
            .Replace("$DESTINATION", expectedFilePath.Replace('\\', '/'));

        await File.WriteAllTextAsync(tempFile, configText);

        var loggerFactory = NullLoggerFactory.Instance;
        var dataRequest = new DataRequestService(loggerFactory);
        var commandExecution = new CommandExecutionService(loggerFactory);
        var registry = new FileInfo(tempFile);
        var config = new FileInfo(@"Configs/DownloaderConfig.json");
        var parameters = new Dictionary<string, string>();

        var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, registry, config, parameters, false);
        await downloader.ExecuteAsync();

        Assert.True(File.Exists(expectedFile));
        File.Delete(expectedFile);
    }

    private static (Mock<IDataRequestService> DataRequest, Mock<ICommandExecutionService> CommandExecute, ComponentDownloader Downloader) Initialize(string registryFile, string? downloaderFile = null, Dictionary<string, string>? parameters = null)
    {
        var dataRequest = new Mock<IDataRequestService>();
        var commandExecute = new Mock<ICommandExecutionService>();

        var registryFileInfo = new FileInfo(registryFile);
        var downloaderFileInfo = string.IsNullOrWhiteSpace(downloaderFile) == false
            ? new FileInfo(downloaderFile)
            : null;

        parameters ??= new Dictionary<string, string>();
        var downloader = new ComponentDownloader(NullLoggerFactory.Instance, dataRequest.Object, commandExecute.Object, registryFileInfo, downloaderFileInfo, parameters, false);
        return (dataRequest, commandExecute, downloader);
    }
}
