using Abs.CommonCore.Installer.Actions.Installer;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests
{
    public class ComponentInstallerTests
    {
        [Fact]
        public void InvalidConfig_ThrowsException()
        {
            Assert.Throws<ConfigException>(() => Initialize(@"Configs/Invalid_InstallerConfig.json"));
        }

        [Fact]
        public async Task InvalidConfigValues_ThrowsException()
        {
            var initializer = Initialize(@"Configs/Invalid2_InstallerConfig.json");
            await Assert.ThrowsAsync<Exception>(() => initializer.Installer.ExecuteAsync());
        }

        [Fact]
        public async Task ValidConfig_InstallAction()
        {
            var initializer = Initialize(@"Configs/InstallerConfig.json");
            await initializer.Installer.ExecuteAsync();

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("docker", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ValidConfig_ExecuteAction()
        {
            var initializer = Initialize(@"Configs/InstallerConfig.json");
            await initializer.Installer.ExecuteAsync();

            initializer.CommandExecute.Verify(x => x.ExecuteCommandAsync("dir", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private (Mock<ICommandExecutionService> CommandExecute, ComponentInstaller Installer) Initialize(string file)
        {
            var commandExecute = new Mock<ICommandExecutionService>();

            var fileInfo = new FileInfo(file);
            var downloader = new ComponentInstaller(NullLoggerFactory.Instance, commandExecute.Object, fileInfo);

            return (commandExecute, downloader);
        }
    }
}
