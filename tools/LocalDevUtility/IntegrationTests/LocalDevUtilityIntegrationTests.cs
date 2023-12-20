using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Abs.CommonCore.LocalDevUtility.IntegrationTests.Fixture;
using Abs.CommonCore.LocalDevUtility.Tests.Fixture;
using FluentAssertions;
using Spectre.Console;
using Xunit;
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
    [InlineData("--rabbitmq-local i", new[] { "cc.rabbitmq-local" })]
    // [InlineData("--drex-file-service i", new[] { "cc.drex-file-service", "cc.drex-message-service", "cc.rabbitmq-local", "cc.vector-site" })]
    // [InlineData("--deps i --drex-message-service i --log-viz i", new []{"cc.vector", "cc.rabbitmq-local", "cc.rabbitmq-remote", "cc.grafana", "cc.loki", "cc.drex-message-service"})]
    // [InlineData("--disco-service i", new[] { "cc.disco-service", "cc.rabbitmq-local", "cc.vector-site" })]
    // [InlineData("--siemens-adapter i", new[] { "cc.siemens-adapter", "cc.disco-service", "cc.rabbitmq-local", "cc.vector-site" })]
    public async Task Utility_GivenValidRunCommand_ShouldStartExpectedComposeServices(string componentParameters, string[] expectedServices)
    {
        // Arrange
        var fixture = new LocalDevUtilityFixture(_testOutput);
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var utilityExecutablePath = Path.Combine(executingPath, "Abs.CommonCore.LocalDevUtility.exe");

        await LocalDevUtilityFixture.SetUpValidTestConfig();

        var runCommand = $"{utilityExecutablePath} run {componentParameters} --background --reset --mode r";
        var stopCommand = $"{utilityExecutablePath} stop {componentParameters} --reset";

        // Stop first just in case something was running before we started
        _testOutput.WriteLine("\n\n\n Stopping everything via \"stop\" command before running test");
        fixture.RealPowerShellAdapter.RunPowerShellCommand(stopCommand, fixture.Logger, TimeSpan.FromMinutes(6));

        try
        {
            // Act
            _testOutput.WriteLine("\n\n\n Executing run command");
            var runCommandOutput = fixture.RealPowerShellAdapter.RunPowerShellCommand(runCommand, fixture.Logger, TimeSpan.FromMinutes(6));

            // Assert
            var composeUpCommand = runCommandOutput.Single(s => s.Contains("docker-compose ") && s.Contains(" up "));
            var upIndex = composeUpCommand.IndexOf(" up ", StringComparison.InvariantCultureIgnoreCase);
            var composeCommandPart = composeUpCommand[..upIndex];

            var statusCommand = $"{composeCommandPart} ps --all --format json";
            var stopwatch = Stopwatch.StartNew();
            var allServicesAreStoodUp = false;
            Exception? lastException = null;
            while (!allServicesAreStoodUp && stopwatch.Elapsed < TimeSpan.FromSeconds(180))
            {
                try
                {
                    var statusCommandRawResult = fixture.RealPowerShellAdapter.RunPowerShellCommand(statusCommand, TimeSpan.FromMinutes(2));
                    var statusCommandJsonResult = JsonSerializer.Deserialize<List<DockerComposeStatusItem>>(string.Join("\n", statusCommandRawResult));

                    statusCommandJsonResult.Should().NotBeNull();
                    statusCommandJsonResult.Should().HaveCount(expectedServices.Length);

                    if (statusCommandJsonResult != null)
                    {
                        _testOutput.WriteLine("TestOutput Project: {0}", string.Join(" ", statusCommandJsonResult.Select(i => i.Project).ToArray()));
                    }

                    expectedServices.Should().AllSatisfy(s => statusCommandJsonResult!.Should().Contain(j => j.Service == s));
                    statusCommandJsonResult!
                        .Where(i => i.Project == "abs-cc" || i.Labels?.IndexOf("com.docker.compose.project=abs-cc") >= 0).Should()
                        .AllSatisfy(i =>
                        {
                            i.State.Should().BeOneOf("running", "created");
                            i.Health.Should().BeOneOf("healthy", "starting", string.Empty);
                            i.ExitCode.Should().Be(0);
                        });

                    allServicesAreStoodUp = true;
                    lastException = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("--Exception--");
                    Console.WriteLine(ex.Message);
                    lastException = ex;
                }
            }

            lastException.Should().BeNull();
        }
        finally
        {
            _testOutput.WriteLine("\n\n\n");
            fixture.RealPowerShellAdapter.RunPowerShellCommand(stopCommand, fixture.Logger, TimeSpan.FromMinutes(6));
        }
    }
}
