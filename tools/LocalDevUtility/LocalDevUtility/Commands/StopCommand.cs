using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.LocalDevUtility.Models;

namespace Abs.CommonCore.LocalDevUtility.Commands;

public static class StopCommand
{
    public static async Task<int> Stop(StopOptions stopOptions)
    {
        using (CliStep.Start("Stopping compose services"))
        {
            PowerShellHelper.RunPowerShellCommand("docker-compose stop");
        }

        if (stopOptions.Reset == true)
        {
            DockerHelper.ResetDocker();
        }

        return 0;
    }
}
