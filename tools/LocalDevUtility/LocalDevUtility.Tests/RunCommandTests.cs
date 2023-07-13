using Abs.CommonCore.LocalDevUtility.Helpers;
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
    [InlineData("run -m r --vector i", new[]{"cc.rabbitmq-local", "cc.vector"})]
    [InlineData("run -m r --grafana i", new[]{"cc.grafana", "cc.loki"})]
    [InlineData("run -m r --loki i", new[]{"cc.loki"})]
    [InlineData("run -m r --deps", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector"})]
    [InlineData("run -m r --deps --drex-service i", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector", "cc.drex-service"})]
    [InlineData("run -m r --deps --log-viz --drex-service i", new[]{"cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector", "cc.drex-service", "cc.loki", "cc.grafana"})]
    public async Task RunCommand_GivenValidInput_ShouldExecuteDockerCompose(string command, string[] expectedServices)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        await fixture.SetUpConfig();

        // Act
        await fixture.ExecuteApplication(command);

        // Assert
        fixture.ActualPowerShellCommands.Should().HaveCountGreaterThan(0);
        var composeUpCommand = fixture.ActualPowerShellCommands.Single(_ => _.Contains("docker-compose ") && _.Contains(" up "));
        var upIndex = composeUpCommand.IndexOf(" up ", StringComparison.InvariantCultureIgnoreCase);
        var composeCommandPart = composeUpCommand.Substring(0, upIndex);

        var realPowerShellAdapter = new PowerShellAdapter();

        var configCommand = $"{composeCommandPart} config";
        var configCommandOutput = realPowerShellAdapter.RunPowerShellCommand(configCommand);
        configCommandOutput.Should().HaveCountGreaterThan(0);
        configCommandOutput.First().Should().Be("name: abs-cc");

        var configServicesCommand = $"{configCommand} --services";
        var configServicesCommandOutput = realPowerShellAdapter.RunPowerShellCommand(configServicesCommand);
        configServicesCommandOutput.Should().HaveCount(expectedServices.Length);
        configServicesCommandOutput.Should().AllSatisfy(_ => expectedServices.Should().Contain(_));
    }
}
