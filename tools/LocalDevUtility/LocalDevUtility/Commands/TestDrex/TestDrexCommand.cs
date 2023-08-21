using System.Text;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.LocalDevUtility.Commands.TestDrex;
public static class TestDrexCommand
{
    public static async Task<int> Run(TestDrexOptions testDrexOptions, ILogger logger,
        IPowerShellAdapter powerShellAdapter)
    {
        var appConfig = (await ConfigureCommand.ReadConfig())!;
        ConfigureCommand.ValidateConfigAndThrow(appConfig);

        var testClientFolder = Path.Combine("client", "TestClient");
        var testClientPath = Path.Combine(appConfig.CommonCoreDrexRepositoryPath!, testClientFolder);

        const string configuration = "Release";
        var publishOutputPath = Path.Combine(testClientPath, "bin", configuration, "LocalDev");
        var publishCommand = $"dotnet publish {testClientPath} -c {configuration} -o {publishOutputPath}";
        powerShellAdapter.RunPowerShellCommand(publishCommand);

        const string testAppName = "Abs.CommonCore.Drex.TestClient.exe";
        var testAppFullName = Path.Combine(publishOutputPath, testAppName);

        var executeTestDrexCommandBuilder = new StringBuilder(testAppFullName);

        if (testDrexOptions.Config is not null)
        {
            executeTestDrexCommandBuilder.Append($" -c {testDrexOptions.Config}");
        }

        if (testDrexOptions.Loop is true)
        {
            executeTestDrexCommandBuilder.Append(" -l");
        }

        if (testDrexOptions.Role is not null)
        {
            executeTestDrexCommandBuilder.Append($" -r {testDrexOptions.Role}");
        }

        if (testDrexOptions.Origin is not null)
        {
            executeTestDrexCommandBuilder.Append($" -o {testDrexOptions.Origin}");
        }

        powerShellAdapter.RunPowerShellCommand(executeTestDrexCommandBuilder.ToString());
        Console.WriteLine("Finished.");

        return 0;
    }
}
