using Abs.CommonCore.Installer.Actions.Downloader;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests
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
            Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_DownloaderConfig.json"));
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
        public async Task ValidConfig_CommandExecuted()
        {
            var initializer = Initialize(@"Configs/RegistryConfig.json");
            await initializer.Downloader.ExecuteAsync(new[] { "RabbitMq" });

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        private (Mock<IDataRequestService> DataRequest, Mock<ICommandExecutionService> CommandExecute, ComponentDownloader Downloader) Initialize(string registryFile, string? downloaderFile = null)
        {
            var dataRequest = new Mock<IDataRequestService>();
            var commandExecute = new Mock<ICommandExecutionService>();

            var registryFileInfo = new FileInfo(registryFile);
            var downloaderFileInfo = string.IsNullOrWhiteSpace(downloaderFile) == false
                ? new FileInfo(downloaderFile)
                : null;

            var downloader = new ComponentDownloader(NullLoggerFactory.Instance, dataRequest.Object, commandExecute.Object, registryFileInfo, downloaderFileInfo);
            return (dataRequest, commandExecute, downloader);
        }
    }
}
