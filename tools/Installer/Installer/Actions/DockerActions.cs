using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Polly;

namespace Abs.CommonCore.Installer.Actions;
public class DockerActions(ICommandExecutionService commandExecutionService, ILogger logger) : IDockerActions
{
    public bool WaitForDockerContainersHealthy { get; set; } = true;

    public async Task<bool> IsDockerRunning(string dockerPath)
    {
        var dockerRun = true;

        try
        {
            await commandExecutionService.ExecuteCommandAsync(dockerPath, "ps", "");
        }
        catch
        {
            dockerRun = false;
            logger.LogInformation("Docker is not running.");
        }

        return dockerRun;
    }

    public async Task RunDockerComposeCommandAsync(Component component, string rootLocation, ComponentAction action)
    {
        logger.LogInformation($"{component.Name}: Running docker compose for '{action.Source}'");
        await ExecuteDockerComposeAsync(rootLocation, action);
    }

    public async Task StopAllContainersAsync()
    {
        var dockerPath = DockerPath.GetDockerPath();
        await commandExecutionService.ExecuteCommandAsync("powershell",
                                                           $"-Command \"{dockerPath} stop $({dockerPath} ps -a -q)\" 2>&1", "");
    }

    public async Task DockerSystemPruneAsync()
    {
        var dockerPath = DockerPath.GetDockerPath();
        await commandExecutionService.ExecuteCommandAsync(dockerPath, "system prune -f", "");
        await commandExecutionService.ExecuteCommandAsync(dockerPath, "network prune -f", "");
    }

    public async Task ExecuteDockerComposeAsync(string rootLocation, ComponentAction action)
    {
        var configFiles = string.IsNullOrWhiteSpace(action.Destination)
            ? Directory.GetFiles(action.Source, "docker-compose.*.yml", SearchOption.AllDirectories)
            : [action.Destination, Path.Combine(action.Source, "docker-compose.root.yml")];

        var envFile = Directory.GetFiles(action.Source, "environment.env", SearchOption.TopDirectoryOnly);

        var arguments = configFiles
                        .Select(x => $"-f {x}")
                        .StringJoin(" ");

        if (envFile.Length == 1)
        {
            arguments = $"--env-file {envFile[0]} " + arguments;
        }

        var dockerComposePath = DockerPath.GetDockerComposePath();
        await commandExecutionService.ExecuteCommandAsync(dockerComposePath, $"{arguments} up --build --detach 2>&1", rootLocation);

        var containerCount = configFiles
            .Count(x => !x.Contains(".root.", StringComparison.OrdinalIgnoreCase));

        if (WaitForDockerContainersHealthy)
        {
            await WaitForDockerContainersHealthyAsync(containerCount, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(10));
        }
    }

    private async Task WaitForDockerContainersHealthyAsync(int totalContainers, TimeSpan totalTime, TimeSpan checkInterval)
    {
        logger.LogInformation($"Waiting for {totalContainers} containers to be healthy");
        var start = DateTime.UtcNow;
        var client = new DockerClientConfiguration()
            .CreateClient();

        while (DateTime.UtcNow.Subtract(start) < totalTime)
        {
            var containers = await LoadContainerInfoAsync(client);
            var healthyCount = 0;

            if (containers.Length == 0)
            {
                logger.LogWarning("No containers found");
            }

            foreach (var container in containers.OrderBy(x => x.Image))
            {
                var isHealthy = CheckContainerHealthy(container, TimeSpan.FromSeconds(30));
                if (isHealthy)
                {
                    healthyCount++;
                }

                var logLevel = isHealthy
                    ? LogLevel.Information
                    : LogLevel.Warning;

                logger.Log(logLevel, $"Container '{container.Name.Trim('/')}': {(isHealthy ? "Healthy" : "Unhealthy")}");
            }

            if (healthyCount >= totalContainers)
            {
                logger.LogInformation("All containers are healthy");
                return;
            }

            await Task.Delay(checkInterval);
        }

        logger.LogError("Not all containers are healthy");
        throw new Exception("Not all containers are healthy");
    }

    private static async Task<ContainerInspectResponse[]> LoadContainerInfoAsync(DockerClient client)
    {
        var containers = await client.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

        var waitAndRetry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt + 1)));

        var containerInfo =
            await containers.SelectAsync(async c =>
                await waitAndRetry.ExecuteAsync(async () => await client.Containers.InspectContainerAsync(c.ID)));

        return containerInfo.ToArray();
    }

    private static bool CheckContainerHealthy(ContainerInspectResponse container, TimeSpan containerHealthyTime)
    {
        var startTime = string.IsNullOrWhiteSpace(container.State.StartedAt)
            ? DateTime.MaxValue
            : DateTime.Parse(container.State.StartedAt).ToUniversalTime();

        return (container.State.Health == null || !string.Equals(container.State.Health.Status, "unhealthy", StringComparison.OrdinalIgnoreCase))
            && container.State.Running && container.State.Restarting == false &&
            ((container.State.Health != null && string.Equals(container.State.Health.Status, "healthy", StringComparison.OrdinalIgnoreCase)) ||
             DateTime.UtcNow.Subtract(startTime) > containerHealthyTime);
    }
}
