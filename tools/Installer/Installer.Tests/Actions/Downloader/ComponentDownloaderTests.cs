using Abs.CommonCore.Installer.Actions.Downloader;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions.Downloader
{
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
            var initializer = Initialize(@"Configs/Invalid2_RegistryConfig.json");
            await Assert.ThrowsAsync<Exception>(() => initializer.Downloader.ExecuteAsync());
        }

        [Fact]
        public async Task InvalidDownloaderConfigValues_ThrowsException()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json", @"Configs/InvalidComponent_DownloaderConfig.json");
            await Assert.ThrowsAsync<Exception>(() => initializer.Downloader.ExecuteAsync());
        }

        [Fact]
        public async Task ValidConfig_FileDownloaded()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json");
            await initializer.Downloader.ExecuteAsync(new[] { "RabbitMq" });

            initializer.DataRequest.Verify(x => x.RequestByteArrayAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ParameterizedConfig_FileDownloaded()
        {
            var paramKey = "$SOME_DOWNLOAD_PARAM";
            var paramValue = "Replacement";

            var parameters = new Dictionary<string, string>() { { paramKey, paramValue } };
            var initializer = Initialize(@"Configs/ParameterizedRegistryConfig.json", parameters: parameters);
            await initializer.Downloader.ExecuteAsync(new[] { "RabbitMq" });

            initializer.DataRequest.Verify(x => x.RequestByteArrayAsync(paramValue), Times.Exactly(1));
        }

        [Fact]
        public async Task ValidConfig_CommandExecuted()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json");
            await initializer.Downloader.ExecuteAsync(new[] { "RabbitMq" });

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        private (Mock<IDataRequestService> DataRequest, Mock<ICommandExecutionService> CommandExecute, ComponentDownloader Downloader) Initialize(string registryFile, string? downloaderFile = null, Dictionary<string, string>? parameters = null)
        {
            var dataRequest = new Mock<IDataRequestService>();
            var commandExecute = new Mock<ICommandExecutionService>();

            var registryFileInfo = new FileInfo(registryFile);
            var downloaderFileInfo = string.IsNullOrWhiteSpace(downloaderFile) == false
                ? new FileInfo(downloaderFile)
                : null;

            parameters ??= new Dictionary<string, string>();
            var downloader = new ComponentDownloader(NullLoggerFactory.Instance, dataRequest.Object, commandExecute.Object, registryFileInfo, downloaderFileInfo, parameters);
            return (dataRequest, commandExecute, downloader);
        }
    }
}
