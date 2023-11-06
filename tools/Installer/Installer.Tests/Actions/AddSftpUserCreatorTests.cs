using Abs.CommonCore.Drex.Shared;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Moq;

namespace Installer.Tests.Actions;

public class AddSftpUserCreatorTests
{
    private const string Name = "some_name";
    private const string ExpectedCommandPrefix = $"exec -it cc.sftp-service cmd.exe /C dotnet Abs.CommonCore.SftpService.dll -u {Name}";
    private const string DrexCommand = "-d";

    [Fact]
    public async Task AddSftpUser_DrexUser_DrexPresent()
    {
        // Arrange
        var commandExecution = new Mock<ICommandExecutionService>();
        var testObj = new AddSftpUser(commandExecution.Object);

        // Act
        await testObj.AddSftpUserAsync(Name, true);

        // Assert
        commandExecution.Verify(x => x.ExecuteCommandAsync(Abs.CommonCore.Installer.Constants.DockerPath,
            It.Is<string>(v => v.StartsWith(ExpectedCommandPrefix) && v.Contains(DrexCommand)), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task AddSftpUser_NonDrexUser_DrexNotPresent()
    {
        // Arrange
        var commandExecution = new Mock<ICommandExecutionService>();
        var testObj = new AddSftpUser(commandExecution.Object);

        // Act
        await testObj.AddSftpUserAsync(Name, false);

        // Assert
        commandExecution.Verify(x => x.ExecuteCommandAsync(Abs.CommonCore.Installer.Constants.DockerPath,
            It.Is<string>(v => v.StartsWith(ExpectedCommandPrefix) && !v.Contains(DrexCommand)), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }
}
