using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests;

public class RunCommandTests
{
    private readonly ITestOutputHelper _testOutput;

    public RunCommandTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Theory]
    [InlineData("run -m r --rabbitmq i", new[]{"cc.rabbitmq-local"})]
    [InlineData("run -m r --rabbitmq-local i", new[]{"cc.rabbitmq-local"})]
    [InlineData("run -m r --rabbitmq-remote i", new[]{"cc.rabbitmq-remote"})]
    [InlineData("run -m r --vector i", new[]{"cc.rabbitmq-local", "cc.vector"})]
    [InlineData("run -m r --grafana i", new[]{"cc.grafana", "cc.loki"})]
    [InlineData("run -m r --loki i", new[]{"cc.loki"})]
    [InlineData("run -m r --drex-service i", new[]{"cc.drex-service", "cc.rabbitmq-local", "cc.vector"})]
    [InlineData("run -m r --deps i", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector"})]
    [InlineData("run -m r --deps i --drex-service i", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector", "cc.drex-service"})]
    [InlineData("run -m r --deps i --log-viz i --drex-service i", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector", "cc.drex-service", "cc.loki", "cc.grafana"})]
    public async Task RunCommand_GivenValidInput_ShouldExecuteDockerCompose(string command, string[] expectedServices)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        await fixture.SetUpConfig();

        // Act
        await fixture.ExecuteApplication(command);

        // Assert
        var composeCommandPart = AssertComposeCommandWasExecutedAndExtractComposeCommandPart(fixture);
        AssertComposeConfigIsValid(fixture, composeCommandPart);
        AssertComposeStartsExpectedServices(fixture, composeCommandPart, expectedServices);
    }

    private string AssertComposeCommandWasExecutedAndExtractComposeCommandPart(LocalDevUtilityFixture fixture)
    {
        fixture.ActualPowerShellCommands.Should().HaveCountGreaterThan(0);
        var composeUpCommand = fixture.ActualPowerShellCommands.Single(_ => _.Contains("docker-compose ") && _.Contains(" up "));
        var upIndex = composeUpCommand.IndexOf(" up ", StringComparison.InvariantCultureIgnoreCase);
        return composeUpCommand.Substring(0, upIndex);
    }

    private void AssertComposeConfigIsValid(LocalDevUtilityFixture fixture, string composeCommandPart)
    {
        var configCommand = $"{composeCommandPart} config";
        var configCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configCommand);
        configCommandOutput.Should().HaveCountGreaterThan(0);
        configCommandOutput.First().Should().Be("name: abs-cc");
    }

    private void AssertComposeStartsExpectedServices(LocalDevUtilityFixture fixture, string composeCommandPart, string[] expectedServices)
    {
        var configServicesCommand = $"{composeCommandPart} config --services";
        var configServicesCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configServicesCommand);
        configServicesCommandOutput.Should().HaveCount(expectedServices.Length);
        configServicesCommandOutput.Should().AllSatisfy(_ => expectedServices.Should().Contain(_));
    }
}
