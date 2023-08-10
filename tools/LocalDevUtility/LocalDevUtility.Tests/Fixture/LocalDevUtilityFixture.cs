using System.Reflection;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Fixture;

public class LocalDevUtilityFixture
{
    public Mock<IPowerShellAdapter> MockPowerShellAdapter { get; set; }

    public PowerShellAdapter RealPowerShellAdapter { get; set; }
    public readonly ILogger Logger;

    public List<string> ActualPowerShellCommands { get; set; } = new();

    public LocalDevUtilityFixture(ITestOutputHelper testOutput)
    {
        MockPowerShellAdapter = new Mock<IPowerShellAdapter>();
        MockPowerShellAdapter
            .Setup(_ => _.RunPowerShellCommand(It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Callback<string, TimeSpan?>((commandItem, _) => { ActualPowerShellCommands.Add(commandItem); });

        TestLogger.Default.SetTestOutput(testOutput);
        Logger = TestLogger.Default;
        RealPowerShellAdapter = new PowerShellAdapter();
    }

    public async Task ExecuteApplication(string input)
    {
        var inputArray = input.Split(" ");
        await Program.Run(inputArray, TestLogger.Default, MockPowerShellAdapter.Object);
    }

    public AppConfig GetValidTestConfig()
    {
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var repoRootPath = Path.GetFullPath(Path.Combine(executingPath, "../../../../../.."));

        var dummyCertPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-certs");
        Directory.CreateDirectory(Path.Combine(dummyCertPath, "local-keys"));
        Directory.CreateDirectory(Path.Combine(dummyCertPath, "local-certs"));

        var dummySftpRootPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-sftp-root");
        Directory.CreateDirectory(Path.Combine(dummySftpRootPath));

        return new AppConfig
        {
            CommonCorePlatformRepositoryPath = repoRootPath,
            CommonCoreDrexRepositoryPath = Path.Combine(repoRootPath, "tools/LocalDevUtility/dummy-cc-drex-repo"),
            ContainerWindowsVersion = "2019",
            CertificatePath = dummyCertPath,
            SftpRootPath = dummySftpRootPath
        };
    }

    public async Task SetUpValidTestConfig()
    {
        await SetUpConfig(GetValidTestConfig());
    }

    public async Task SetUpConfig(AppConfig? config)
    {
        ConfigureCommand.ValidateConfig(config).Should().HaveCount(0);
        await ConfigureCommand.SaveConfig(config);
    }
}
