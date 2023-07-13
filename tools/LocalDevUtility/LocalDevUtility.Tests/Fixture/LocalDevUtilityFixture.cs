using Abs.CommonCore.LocalDevUtility.Commands;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.LocalDevUtility.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Abs.CommonCore.LocalDevUtility.Tests.Fixture;

public class LocalDevUtilityFixture
{
    public Mock<ILogger> MockLogger { get; set; }
    public Mock<IPowerShellAdapter> MockPowerShellAdapter { get; set; }
    public PowerShellAdapter RealPowerShellAdapter { get; set; }

    public List<string> ActualPowerShellCommands { get; set; } = new();

    public LocalDevUtilityFixture()
    {
        MockLogger = new Mock<ILogger>();
        MockPowerShellAdapter = new Mock<IPowerShellAdapter>();
        MockPowerShellAdapter
            .Setup(_ => _.RunPowerShellCommand(It.IsAny<string>()))
            .Callback<string>(commandItem => { ActualPowerShellCommands.Add(commandItem); });

        RealPowerShellAdapter = new PowerShellAdapter();
    }

    public async Task ExecuteApplication(string input)
    {
        var inputArray = input.Split(" ");
        await Program.Main(inputArray, MockLogger.Object, MockPowerShellAdapter.Object);
    }

    public async Task SetUpConfig()
    {
        await SetUpConfig(new AppConfig
        {
            CommonCorePlatformRepositoryPath = "C:/src/abs/cc-platform",
            CommonCoreDrexRepositoryPath = "C:/src/abs/cc-drex",
            ContainerWindowsVersion = "2019"
        });
    }

    public async Task SetUpConfig(AppConfig? config)
    {
        await ConfigureCommand.SaveConfig(config);
    }
}
