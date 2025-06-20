﻿using System.Text;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Helpers;

namespace Abs.CommonCore.LocalDevUtility.Commands.TestDrex;
public static class TestDrexCommand
{
    public static async Task<int> Run(TestDrexOptions testDrexOptions,
        IPowerShellAdapter powerShellAdapter)
    {
        var appConfig = (await ConfigureCommand.ReadConfig())!;
        ConfigureCommand.ValidateConfigAndThrow(appConfig);

        var testClientProj = Path.Combine("client", "TestClient", "TestClient.csproj");
        var testClientPath = Path.Combine(appConfig.CommonCoreDrexRepositoryPath!, testClientProj);

        var restoreCommand = $"dotnet restore {testClientPath}";
        powerShellAdapter.RunPowerShellCommand(restoreCommand);

        const string configuration = "Release";
        var runCommand = $"dotnet run  -c {configuration} --project {testClientPath} -- ";
        var executeTestDrexCommandBuilder = new StringBuilder(runCommand);

        if (!string.IsNullOrWhiteSpace(testDrexOptions.Name))
        {
            executeTestDrexCommandBuilder.Append($" -n {testDrexOptions.Name}");
        }

        if (testDrexOptions.File is not null)
        {
            executeTestDrexCommandBuilder.Append(" -f");
        }

        if (testDrexOptions.Config is not null)
        {
            executeTestDrexCommandBuilder.Append($" -c {testDrexOptions.Config.FullName}");
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

        return 0;
    }
}
