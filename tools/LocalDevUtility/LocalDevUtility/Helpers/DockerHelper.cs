namespace Abs.CommonCore.LocalDevUtility.Helpers;

public static class DockerHelper
{
    public static void ResetDocker()
    {
        using (CliStep.Start("Resetting Docker", true))
        {
            PowerShellHelper.RunPowerShellCommand("docker ps -aq | ForEach-Object { Write-Output \"Stopping $(docker stop $_) & removing $(docker rm $_)\" };");
            PowerShellHelper.RunPowerShellCommand("Write-Output \"Pruning system: $(docker system prune -f)\"");
            PowerShellHelper.RunPowerShellCommand("Write-Output \"Pruning volume: $(docker volume prune -f)\"");
            PowerShellHelper.RunPowerShellCommand("Write-Output \"Pruning network: $(docker network prune -f)\"");
        }
    }
}
