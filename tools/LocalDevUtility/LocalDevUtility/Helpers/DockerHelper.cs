namespace Abs.CommonCore.LocalDevUtility.Helpers;

public static class DockerHelper
{
    public static void ResetDocker(IPowerShellAdapter powerShellAdapter)
    {
        using (CliStep.Start("Resetting Docker", true))
        {
            powerShellAdapter.RunPowerShellCommand("docker ps -aq | ForEach-Object { Write-Output \"Stopping $(docker stop $_) & removing $(docker rm $_)\" };");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning system: $(docker system prune -f)\"");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning volume: $(docker volume prune -f)\"");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning network: $(docker network prune -f)\"");
        }
    }
}
