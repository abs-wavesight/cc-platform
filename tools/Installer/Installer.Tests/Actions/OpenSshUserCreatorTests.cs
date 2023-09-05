using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Services;
using Moq;

namespace Installer.Tests.Actions;

public class OpenSshUserCreatorTests
{
    private const string Name = "some_name";
    private const string ExpectedCommandPrefix = $"exec -it openssh cmd.exe /C pwsh -Command C:\\scripts\\create-drex-user.ps1 -Username {Name}";
    private const string RestartCommand = "restart";

    [Fact]
    public async Task AddOpenSshUser_DrexUser_NoRestartRequested()
    {
        // Arrange
        var commandExecution = new Mock<ICommandExecutionService>();
        var testObj = new OpenSshUserCreator(commandExecution.Object);


        // Act
        await testObj.AddOpenSshUserAsync(Name, true);

        // Assert
        commandExecution.Verify(x => x.ExecuteCommandAsync("docker", It.Is<string>(v => v.StartsWith(ExpectedCommandPrefix)), It.IsAny<string>()), Times.Once);
        commandExecution.Verify(x => x.ExecuteCommandAsync("docker", It.Is<string>(v => v.StartsWith(RestartCommand)), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddOpenSshUser_NonDrexUser_RestartRequested()
    {
        // Arrange
        var commandExecution = new Mock<ICommandExecutionService>();
        var testObj = new OpenSshUserCreator(commandExecution.Object);


        // Act
        await testObj.AddOpenSshUserAsync(Name, false);

        // Assert
        commandExecution.Verify(x => x.ExecuteCommandAsync("docker", It.Is<string>(v => v.StartsWith(ExpectedCommandPrefix)), It.IsAny<string>()), Times.Once);
        commandExecution.Verify(x => x.ExecuteCommandAsync("docker", It.Is<string>(v => v.StartsWith(RestartCommand)), It.IsAny<string>()), Times.Once);
    }
}
