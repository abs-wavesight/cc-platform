using Abs.CommonCore.LocalDevUtility.Commands;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.LocalDevUtility.Models;
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
            .Setup(_ => _.RunPowerShellCommand(It.IsAny<string>()))
            .Callback<string>(commandItem => { ActualPowerShellCommands.Add(commandItem); });

        TestLogger.Default.SetTestOutput(testOutput);
        Logger = TestLogger.Default;
        RealPowerShellAdapter = new PowerShellAdapter();
    }

    public async Task ExecuteApplication(string input)
    {
        var inputArray = input.Split(" ");
        await Program.Run(inputArray, TestLogger.Default, MockPowerShellAdapter.Object);
    }

    public async Task SetUpConfig()
    {
        await SetUpConfig(new AppConfig
        {
            CommonCorePlatformRepositoryPath = "C:/src/abs/cc-platform", // TODO RH: Fix these values in CI
            CommonCoreDrexRepositoryPath = "C:/src/abs/cc-drex",
            ContainerWindowsVersion = "2019"
        });
    }

    public async Task SetUpConfig(AppConfig? config)
    {
        await ConfigureCommand.SaveConfig(config);
    }
}
