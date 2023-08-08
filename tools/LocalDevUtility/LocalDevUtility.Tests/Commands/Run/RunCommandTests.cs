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
    [InlineData("run -m r --openssl i", new[] { "cc.openssl-generate-certs" })]
    [InlineData("run -m r --rabbitmq i", new[] { "cc.rabbitmq-local" })]
    [InlineData("run -m r --rabbitmq-local i", new[] { "cc.rabbitmq-local" })]
    [InlineData("run -m r --rabbitmq-remote i", new[] { "cc.rabbitmq-remote" })]
    [InlineData("run -m r --vector i", new[] { "cc.rabbitmq-local", "cc.vector-site" }, new[] { "vector/docker-compose.variant.default.yml" })]
    [InlineData("run -m r --grafana i", new[] { "cc.grafana", "cc.loki", "cc.rabbitmq-local", "cc.vector-site" })]
    [InlineData("run -m r --loki i", new[] { "cc.loki", "cc.rabbitmq-local", "cc.vector-site" }, new[] { "vector/docker-compose.variant.loki.yml" })]
    [InlineData("run -m r --drex-message-service i", new[] { "cc.drex-message-service", "cc.rabbitmq-local", "cc.vector-site" })]
    [InlineData("run -m r --drex-file-service i", new[] { "cc.drex-file-service", "cc.rabbitmq-local", "cc.vector-site", "cc.drex-message-service" })]
    [InlineData("run -m r --deps i", new[] { "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central" })]
    [InlineData("run -m r --deps i --drex-message-service i", new[] { "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central", "cc.drex-message-service" }, new[] { "vector/docker-compose.variant.default.yml" })]
    [InlineData("run -m r --deps i --drex-file-service i", new[] { "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central", "cc.drex-message-service", "cc.drex-file-service" }, new[] { "vector/docker-compose.variant.default.yml" })]
    [InlineData("run -m r --deps i --log-viz i --drex-message-service i", new[] { "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.vector-site", "cc.vector-central", "cc.drex-message-service", "cc.loki", "cc.grafana" }, new[] { "vector/docker-compose.variant.loki.yml" })]
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

    private static string AssertComposeCommandWasExecutedAndExtractComposeCommandPart(LocalDevUtilityFixture fixture)
    {
        fixture.ActualPowerShellCommands.Should().HaveCountGreaterThan(0);
        var composeUpCommand = fixture.ActualPowerShellCommands.Single(s => s.Contains("docker-compose ") && s.Contains(" up "));
        var upIndex = composeUpCommand.IndexOf(" up ", StringComparison.InvariantCultureIgnoreCase);
        return composeUpCommand[..upIndex];
    }

    private void AssertComposeConfigIsValid(LocalDevUtilityFixture fixture, string composeCommandPart)
    {
        var configCommand = $"{composeCommandPart} config";
        var configCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configCommand, TimeSpan.FromMinutes(1));
        _testOutput.WriteLine($"Compose Config Output:{Environment.NewLine}{string.Join(Environment.NewLine, configCommandOutput)}");
        configCommandOutput.Should().HaveCountGreaterThan(0);
        configCommandOutput.First().Should().Be("name: abs-cc");
    }

    private static void AssertComposeStartsExpectedServices(LocalDevUtilityFixture fixture, string composeCommandPart, IReadOnlyCollection<string> expectedServices)
    {
        var configServicesCommand = $"{composeCommandPart} config --services";
        var configServicesCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(configServicesCommand, TimeSpan.FromMinutes(1));
        configServicesCommandOutput.Should().HaveCount(expectedServices.Count);
        configServicesCommandOutput.Should().AllSatisfy(s => expectedServices.Should().Contain(s));
    }

    private static void AssertSpecificExpectedComposeFilesArePresent(string composeCommandPart, IEnumerable<string>? specificExpectedComposeFiles)
    {
        specificExpectedComposeFiles?.Should()
            .AllSatisfy(s => composeCommandPart.Should().Contain(s));
    }
}
