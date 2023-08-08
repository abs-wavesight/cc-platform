
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions
{
    public class UninstallerTests : TestsBase
    {
        [Fact]
        public async Task Configuration_Removed()
        {
            var testPath = Directory.CreateTempSubdirectory("uninstall_test");
            var config = Path.Combine(testPath.FullName, "config");
            Directory.CreateDirectory(config);

            var commandExecution = new Mock<ICommandExecutionService>();
            var uninstaller = new Uninstaller(NullLoggerFactory.Instance, commandExecution.Object);
            await uninstaller.UninstallSystemAsync(null, testPath, null, true, null);

            Assert.True(Directory.Exists(config) == false);
        }

        [Fact]
        public async Task Docker_Removed()
        {
            var testPath = Directory.CreateTempSubdirectory("uninstall_test");

            var commandExecution = new Mock<ICommandExecutionService>();
            var uninstaller = new Uninstaller(NullLoggerFactory.Instance, commandExecution.Object);
            await uninstaller.UninstallSystemAsync(testPath, null, null, null, true);

            Assert.True(Directory.Exists(testPath.FullName) == false);
        }
    }
}
