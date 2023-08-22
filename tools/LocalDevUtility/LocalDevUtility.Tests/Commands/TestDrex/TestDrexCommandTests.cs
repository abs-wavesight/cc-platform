using Abs.CommonCore.LocalDevUtility.Tests.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Commands.TestDrex;

[Collection(nameof(RunCommandTests))]
public class TestDrexCommandTests
{
    private readonly ITestOutputHelper _testOutput;

    public TestDrexCommandTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Theory]
    [InlineData("test-drex -o s -r p", new[] { "-o s", "-r p" })]
    [InlineData("test-drex -o s -r p -l", new[] { "-o s", "-r p", "-l" })]
    [InlineData("test-drex -o c -r p", new[] { "-o c", "-r p" })]
    [InlineData("test-drex -o c -r c -l", new[] { "-o c", "-r c", "-l" })]
    [InlineData("test-drex -l", new[] { "-l" })]
    [InlineData("test-drex -o s", new[] { "-o s" })]
    public async Task RunCommand_GivenValidInput_ShouldExecuteTestClient(string command, string[] expectedOptions)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        await fixture.SetUpValidTestConfig();

        // Act
        await fixture.ExecuteApplication(command);

        // Assert
        fixture.ActualPowerShellCommands.Should().HaveCountGreaterThan(0);
        var exeCommand = fixture.ActualPowerShellCommands[^1];
        exeCommand.Should().ContainAll(expectedOptions);
    }
}
