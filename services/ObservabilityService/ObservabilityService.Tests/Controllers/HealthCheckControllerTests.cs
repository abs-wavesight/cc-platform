using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abs.CommonCore.ObservabilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ObservabilityService.Controllers;
using ObservabilityService.Models;

namespace ObservabilityService.Tests.Controllers;
public class HealthCheckControllerTests
{
    [Fact]
    public async Task GivenGetCall_WhenContainersHealthy_Then200Status()
    {
        var healthService = new Mock<IDockerContainerHealthService>();
        healthService
            .Setup(x => x.GetContainerHealthAsync(It.IsAny<MonitorContainer[]>()))
            .ReturnsAsync("");

        var cache = BuildMemoryCache();
        var containers = new[]
        {
            new MonitorContainer { Name = "xyz" }
        };

        var controller = new HealthCheckController(NullLogger<HealthCheckController>.Instance, cache, healthService.Object, containers);
        var result = await controller.GetAsync();

        Assert.True(result.StatusCode == 200 && result?.Value?.ToString() == "All containers are healthy");
    }

    [Fact]
    public async Task GivenGetCall_WhenContainersUnhealthy_Then503Status()
    {
        var healthService = new Mock<IDockerContainerHealthService>();
        healthService
            .Setup(x => x.GetContainerHealthAsync(It.IsAny<MonitorContainer[]>()))
            .ReturnsAsync("Container 'xyz' is not healthy");

        var cache = BuildMemoryCache();
        var containers = new[]
        {
            new MonitorContainer { Name = "xyz" }
        };

        var controller = new HealthCheckController(NullLogger<HealthCheckController>.Instance, cache, healthService.Object, containers);
        var result = await controller.GetAsync();

        Assert.True(result.StatusCode == 503 && result?.Value is ProblemDetails { Detail: "Container 'xyz' is not healthy" });
    }

    [Fact]
    public async Task GivenGetCall_WhenCalledMultipleTimes_ThenResultsCached()
    {
        var healthService = new Mock<IDockerContainerHealthService>();
        healthService
            .Setup(x => x.GetContainerHealthAsync(It.IsAny<MonitorContainer[]>()))
            .ReturnsAsync("");

        var cache = BuildMemoryCache();
        var containers = new[]
        {
            new MonitorContainer { Name = "xyz" }
        };

        var controller = new HealthCheckController(NullLogger<HealthCheckController>.Instance, cache, healthService.Object, containers);
        await controller.GetAsync();
        await controller.GetAsync();
        await controller.GetAsync();

        healthService
            .Verify(x => x.GetContainerHealthAsync(It.IsAny<MonitorContainer[]>()), Times.Once);
    }

    private static IMemoryCache BuildMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetService<IMemoryCache>()!;
    }
}
