using System.Text.Json;
using Abs.CommonCore.ObservabilityService.Services;
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
    private readonly IDockerContainerHealthService _healthService;
    private readonly MonitorContainer[] _containersToCheck;

    private const string CacheKey = "Health_Check";
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(1);

    public HealthCheckController(ILogger<HealthCheckController> logger, IMemoryCache cache, IDockerContainerHealthService healthService, MonitorContainer[] containersToCheck)
    {
        _logger = logger;
        _cache = cache;
        _healthService = healthService;
        _containersToCheck = containersToCheck;
    }

    [HttpGet]
    public async Task<ObjectResult> GetAsync()
    {
        var result = await _cache.GetOrCreateAsync<string>(CacheKey,
            async e =>
            {
                var health = await _healthService.GetContainerHealthAsync(_containersToCheck);
                e.AbsoluteExpirationRelativeToNow = _cacheTime;
                e.Value = health;

                return health;
            });

        return string.IsNullOrWhiteSpace(result) ? Ok("All containers are healthy") : Problem(statusCode: 503, detail: result);
    }
}
