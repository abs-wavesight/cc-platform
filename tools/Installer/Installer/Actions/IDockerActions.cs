using Abs.CommonCore.Contracts.Json.Installer;

namespace Abs.CommonCore.Installer.Actions;
public interface IDockerActions
{
    bool WaitForDockerContainersHealthy { get; set; }

    Task DockerSystemPruneAsync();
    Task ExecuteDockerComposeAsync(string rootLocation, ComponentAction action);
    Task RunDockerComposeCommandAsync(Component component, string rootLocation, ComponentAction action);
    Task StopAllContainersAsync();
    Task<bool> IsDockerRunning(string dockerPath);
}
