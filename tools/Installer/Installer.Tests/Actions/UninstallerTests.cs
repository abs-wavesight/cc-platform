
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions
{
    public class UninstallerTests : TestsBase
    {
        [Fact]
        public async Task Uninstall_Configuration_ConfigurationRemoved()
        {
            var testPath = Directory.CreateTempSubdirectory("uninstall_test");
            var config = Path.Combine(testPath.FullName, "config");
            Directory.CreateDirectory(config);

            var commandExecution = new Mock<ICommandExecutionService>();
            var uninstaller = new Uninstaller(NullLoggerFactory.Instance, commandExecution.Object);
            await uninstaller.UninstallSystemAsync(null, testPath, null, true, null);

            Assert.False(Directory.Exists(config));
        }

        [Fact]
        public async Task Uninstall_Docker_DockerRemoved()
        {
            var testPath = Directory.CreateTempSubdirectory("uninstall_test");

            var commandExecution = new Mock<ICommandExecutionService>();
            var uninstaller = new Uninstaller(NullLoggerFactory.Instance, commandExecution.Object);
            await uninstaller.UninstallSystemAsync(testPath, null, null, null, true);

            Assert.False(Directory.Exists(testPath.FullName));
            commandExecution.Verify(x => x.ExecuteCommandAsync("net", "stop dockerd", It.IsAny<string>()), Times.Once);
            commandExecution.Verify(x => x.ExecuteCommandAsync("sc", "delete dockerd", It.IsAny<string>()), Times.Once);
        }
    }
}
