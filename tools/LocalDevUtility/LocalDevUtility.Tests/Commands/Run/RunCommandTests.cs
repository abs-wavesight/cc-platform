using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Abs.CommonCore.LocalDevUtility.Tests.Commands.Run;

[Collection(nameof(RunCommandTests))]
public class RunCommandTests
{
    private readonly ITestOutputHelper _testOutput;

    public RunCommandTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Theory]
    [InlineData("run -m r --rabbitmq i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local" })]
    [InlineData("run -m r --rabbitmq-local i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local" })]
    [InlineData("run -m r --rabbitmq-remote i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-remote" })]
    [InlineData("run -m r --vector i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local", "cc.vector-site" }, new[] { "vector/docker-compose.variant.default.yml" })]
    [InlineData("run -m r --grafana i --openssl s", new[] { "cc.openssl-generate-certs", "cc.grafana", "cc.loki", "cc.rabbitmq-local", "cc.vector-site" })]
    [InlineData("run -m r --loki i --openssl s", new[] { "cc.openssl-generate-certs", "cc.loki", "cc.rabbitmq-local", "cc.vector-site" }, new[] { "vector/docker-compose.variant.loki.yml" })]
    [InlineData("run -m r --drex-service i --openssl s", new[] { "cc.openssl-generate-certs", "cc.drex-service", "cc.rabbitmq-local", "cc.vector-site" })]
    [InlineData("run -m r --deps i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central" })]
    [InlineData("run -m r --deps i --drex-service i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central", "cc.drex-service" }, new[] { "vector/docker-compose.variant.default.yml" })]
    [InlineData("run -m r --deps i --log-viz i --drex-service i --openssl s", new[] { "cc.openssl-generate-certs", "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central", "cc.drex-service", "cc.loki", "cc.grafana" }, new[] { "vector/docker-compose.variant.loki.yml" })]
    public async Task RunCommand_GivenValidInput_ShouldExecuteDockerCompose(string command, string[] expectedServices, string[]? specificExpectedComposeFiles = null)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        await fixture.SetUpValidTestConfig();

        // Act
        await fixture.ExecuteApplication(command);

        // Assert
        var composeCommandPart = AssertComposeCommandWasExecutedAndExtractComposeCommandPart(fixture);
        AssertComposeConfigIsValid(fixture, composeCommandPart);
        AssertComposeStartsExpectedServices(fixture, composeCommandPart, expectedServices);
        AssertSpecificExpectedComposeFilesArePresent(composeCommandPart, specificExpectedComposeFiles);
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
        var configCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configCommand, TimeSpan.FromMinutes(1));
        _testOutput.WriteLine($"Compose Config Output:\n{string.Join("\n", configCommandOutput)}");
        configCommandOutput.Should().HaveCountGreaterThan(0);
        configCommandOutput.First().Should().Be("name: abs-cc");
    }

    private void AssertComposeStartsExpectedServices(LocalDevUtilityFixture fixture, string composeCommandPart, string[] expectedServices)
    {
        var configServicesCommand = $"{composeCommandPart} config --services";
        var configServicesCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configServicesCommand, TimeSpan.FromMinutes(1));
        configServicesCommandOutput.Should().HaveCount(expectedServices.Length);
        configServicesCommandOutput.Should().AllSatisfy(_ => expectedServices.Should().Contain(_));
    }

    private void AssertSpecificExpectedComposeFilesArePresent(string composeCommandPart, string[]? specificExpectedComposeFiles)
    {
        if (specificExpectedComposeFiles == null)
        {
            return;
        }

        specificExpectedComposeFiles.Should().AllSatisfy(_ => composeCommandPart.Should().Contain(_));
    }
}
