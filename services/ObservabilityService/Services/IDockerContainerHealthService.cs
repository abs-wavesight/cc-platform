using ObservabilityService.Models;

namespace Abs.CommonCore.ObservabilityService.Services;

public interface IDockerContainerHealthService
{
    Task<string> GetContainerHealthAsync(MonitorContainer[] containersToCheck);
}
