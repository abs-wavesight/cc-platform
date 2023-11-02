using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using ObservabilityService.Models;

namespace Abs.CommonCore.ObservabilityService.Services;

[ExcludeFromCodeCoverage]
public class DockerContainerHealthService : IDockerContainerHealthService
{
    private const string ContainerHealthyState = "running";
    private const string ContainerHealthyStatus = "healthy";

    public async Task<string> GetContainerHealthAsync(MonitorContainer[] containersToCheck)
    {
        using var client = new DockerClientConfiguration().CreateClient();
        var containers = (await client.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                All = true
            }))
            .ToArray();

        var errors = new List<string>();

        foreach (var checkItem in containersToCheck)
        {
            var match = containers.FirstOrDefault(x => x.Names.Any(y => y.Contains(checkItem.Name, StringComparison.OrdinalIgnoreCase)));

            if (match == null && !checkItem.IsRequired)
            {
                continue;
            }

            if (match == null)
            {
                errors.Add($"Container '{checkItem.Name}' is not found");
                continue;
            }

            var isHealthy = match.State.Contains(ContainerHealthyState, StringComparison.OrdinalIgnoreCase) &&
                match.Status.Contains(ContainerHealthyStatus, StringComparison.OrdinalIgnoreCase);

            if (!isHealthy)
            {
                errors.Add($"Container '{checkItem.Name}' is not healthy");
            }
        }

        return errors.StringJoin("\r\n");
    }
}
