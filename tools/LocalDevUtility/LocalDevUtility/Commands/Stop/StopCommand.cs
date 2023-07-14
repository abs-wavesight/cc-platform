using Abs.CommonCore.LocalDevUtility.Helpers;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.LocalDevUtility.Commands.Stop;

public static class StopCommand
{
    public static int Stop(StopOptions stopOptions, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        using (CliStep.Start("Stopping compose services"))
        {
            powerShellAdapter.RunPowerShellCommand("docker-compose stop");
        }

        if (stopOptions.Reset == true)
        {
            DockerHelper.ResetDocker(powerShellAdapter);
        }

        return 0;
    }
}
