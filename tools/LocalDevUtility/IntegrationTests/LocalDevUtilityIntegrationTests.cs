using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Abs.CommonCore.LocalDevUtility.IntegrationTests.Fixture;
using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.IntegrationTests;

public class LocalDevUtilityIntegrationTests
{
    private readonly ITestOutputHelper _testOutput;

    public LocalDevUtilityIntegrationTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Theory]
    [InlineData("--rabbitmq-local i", new []{"cc.rabbitmq-local"})]
    // [InlineData("--deps i --drex-service i --log-viz i", new []{"cc.vector", "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.grafana", "cc.loki", "cc.drex-service"})]
    public async Task Utility_GivenValidRunCommand_ShouldStartExpectedComposeServices(string componentParameters, string[] expectedServices)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var utilityExecutablePath = Path.Combine(executingPath, "Abs.CommonCore.LocalDevUtility.exe");

        // TODO RH: Ideally we should use the actual "configure" command here
        await fixture.SetUpValidTestConfig();

        var runCommand = $"{utilityExecutablePath} run {componentParameters} --background --reset --mode r";
        var stopCommand = $"{utilityExecutablePath} stop {componentParameters} --reset";

        // Stop first just in case something was running before we started
        _testOutput.WriteLine("\n\n\n Stopping everything via \"stop\" command before running test");
        fixture.RealPowerShellAdapter.RunPowerShellCommand(stopCommand, fixture.Logger, TimeSpan.FromMinutes(3));

        try
        {
            // Act
            _testOutput.WriteLine("\n\n\n Executing run command");
            var runCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(runCommand, fixture.Logger, TimeSpan.FromMinutes(3));

            // Assert
            var composeUpCommand = runCommandOutput.Single(_ => _.Contains("docker-compose ") && _.Contains(" up "));
            var upIndex = composeUpCommand.IndexOf(" up ", StringComparison.InvariantCultureIgnoreCase);
            var composeCommandPart = composeUpCommand.Substring(0, upIndex);

            var statusCommand = $"{composeCommandPart} ps --all --format json";
            var stopwatch = Stopwatch.StartNew();
            var allServicesAreStoodUp = false;
            Exception? lastException = null;
            while (!allServicesAreStoodUp && stopwatch.Elapsed < TimeSpan.FromSeconds(180))
            {
                try
                {
                    var statusCommandRawResult = fixture.RealPowerShellAdapter.RunPowerShellCommand(statusCommand, TimeSpan.FromMinutes(1));
                    var statusCommandJsonResult = JsonSerializer.Deserialize<List<DockerComposeStatusItem>>(string.Join("\n", statusCommandRawResult));

                    statusCommandJsonResult.Should().NotBeNull();
                    statusCommandJsonResult.Should().HaveCount(expectedServices.Length);
                    expectedServices.Should().AllSatisfy(_ => statusCommandJsonResult!.Should().Contain(j => j.Service == _));
                    statusCommandJsonResult!
                        .Where(_ => _.Project == "abs-cc").Should()
                        .AllSatisfy(_ =>
                        {
                            _.State.Should().Be("running");
                            _.Health.Should().BeOneOf("healthy", string.Empty);
                            _.ExitCode.Should().Be(0);
                        });

                    allServicesAreStoodUp = true;
                    lastException = null;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            lastException.Should().BeNull();
        }
        finally
        {
            _testOutput.WriteLine("\n\n\n");
            fixture.RealPowerShellAdapter.RunPowerShellCommand(stopCommand, fixture.Logger, TimeSpan.FromMinutes(3));
        }
    }
}
