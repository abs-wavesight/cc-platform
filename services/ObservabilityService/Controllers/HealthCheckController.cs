using System.Text.Json;
using Abs.CommonCore.Platform.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ObservabilityService.Models;

namespace ObservabilityService.Controllers;
[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ILogger<HealthCheckController> _logger;
    private readonly IMemoryCache _cache;
    private readonly MonitorContainer[] _containersToCheck;

    private const string CacheKey = "Health_Check";
    private const string ContainerHealthyState = "running";
    private const string ContainerHealthyStatus = "healthy";
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(1);

    public HealthCheckController(ILogger<HealthCheckController> logger, IMemoryCache cache, MonitorContainer[] containersToCheck)
    {
        _logger = logger;
        _cache = cache;
        _containersToCheck = containersToCheck;
    }

    [HttpGet]
    public async Task<ObjectResult> GetAsync()
    {
        var result = await _cache.GetOrCreateAsync<string>(CacheKey,
            async e =>
            {
                var health = await LoadCurrentHealthAsync();
                e.AbsoluteExpirationRelativeToNow = _cacheTime;
                e.Value = health;

                return health;
            });

        return string.IsNullOrWhiteSpace(result) ? Ok("All containers are healthy") : Problem(statusCode: 503, detail: result);
    }

    private async Task<string> LoadCurrentHealthAsync()
    {
        using var client = new DockerClientConfiguration().CreateClient();
        var containers = (await client.Containers
            .ListContainersAsync(new ContainersListParameters
            {
                All = true
            }))
            .ToArray();

        var errors = new List<string>();

        foreach (var checkItem in _containersToCheck)
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
