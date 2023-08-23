using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.LocalDevUtility.Commands.Stop;

public static class StopCommand
{
    public static async Task<int> Stop(StopOptions stopOptions, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var appConfig = (await ConfigureCommand.ReadConfig())!;
        ConfigureCommand.ValidateConfigAndThrow(appConfig);

        await DockerHelper.CreateEnvFile(appConfig);

        var composeCommandBuilder = DockerHelper.BuildComposeCommand(appConfig, stopOptions);
        composeCommandBuilder.Append(" down");

        Console.WriteLine("Now running the following Docker Compose command:");
        Console.WriteLine(composeCommandBuilder.ToString());
        await powerShellAdapter.RunPowerShellCommandAsync(composeCommandBuilder.ToString());

        if (stopOptions.Reset == true)
        {
            await DockerHelper.ResetDockerAsync(powerShellAdapter);
        }

        return 0;
    }
}
