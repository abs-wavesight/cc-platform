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
        public async Task ValidConfig_SuccessfulExecution()
        {
            var initializer = Initialize(@"Configs/DownloaderConfig.json");
            await initializer.downloader.ExecuteAsync();
        }

        [Fact]
        public void InvalidConfig_ThrowsException()
        {
            Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_DownloaderConfig.json"));
        }

        [Fact]
        public async Task InvalidConfigValues_ThrowsException()
        {
            var initializer = Initialize(@"Configs/Invalid2_DownloaderConfig.json");
            await Assert.ThrowsAsync<Exception>(() => initializer.downloader.ExecuteAsync());
        }

        [Fact]
        public async Task ValidConfig_FileDownloaded()
        {
            var initializer = Initialize(@"Configs/DownloaderConfig.json");
            await initializer.downloader.ExecuteAsync();

            initializer.dataRequest.Verify(x => x.RequestByteArrayAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ValidConfig_CommandExecuted()
        {
            var initializer = Initialize(@"Configs/DownloaderConfig.json");
            await initializer.downloader.ExecuteAsync();

            initializer.commandExecute.Verify(x => x.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task VerifyOnly_FileNotDownloaded()
        {
            var dataRequest = new DataRequestService(NullLogger.Instance, true);
            var result = await dataRequest.RequestByteArrayAsync("http://Not.a.valid.url.path");
            Assert.Equal(Array.Empty<byte>(), result);
        }

        [Fact]
        public async Task ValidConfig__VerifyOnly_CommandNotExecuted()
        {
            var commandExecution = new CommandExecutionService(NullLogger.Instance, true);

            var exception = await Record.ExceptionAsync(() =>
                commandExecution.ExecuteCommandAsync("Not a valid command", "Not a valid argument"));
            Assert.Null(exception);
        }

        private (Mock<IDataRequestService> dataRequest, Mock<ICommandExecutionService> commandExecute, ComponentDownloader downloader) Initialize(string file)
        {
            var dataRequest = new Mock<IDataRequestService>();
            var commandExecute = new Mock<ICommandExecutionService>();

            var fileInfo = new FileInfo(file);
            var downloader = new ComponentDownloader(NullLoggerFactory.Instance, dataRequest.Object, commandExecute.Object, fileInfo);

            return (dataRequest, commandExecute, downloader);
        }
    }
}