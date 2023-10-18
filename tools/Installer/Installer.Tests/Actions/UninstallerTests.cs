
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Installer.Tests.Actions;

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

        Directory.Exists(config).Should().BeFalse();
    }

    [Fact]
    public async Task Uninstall_Docker_DockerRemoved()
    {
        var testPath = Directory.CreateTempSubdirectory("uninstall_test");

        var commandExecution = new Mock<ICommandExecutionService>();
        var uninstaller = new Uninstaller(NullLoggerFactory.Instance, commandExecution.Object);
        await uninstaller.UninstallSystemAsync(testPath, null, null, null, true);

        Directory.Exists(testPath.FullName).Should().BeFalse();
    }
}
