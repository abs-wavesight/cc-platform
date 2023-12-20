using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Abs.CommonCore.Drex.Shared.Infrastructure.Hosting;

public class BasicHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
