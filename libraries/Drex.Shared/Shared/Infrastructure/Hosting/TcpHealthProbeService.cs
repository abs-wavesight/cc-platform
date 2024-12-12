using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Abs.CommonCore.Drex.Shared.Infrastructure.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Drex.Shared.Infrastructure.Hosting;

/*
 * Note: Though technically this class _could_ be unit-tested, it is not worth the effort and code it would require.
 * This class uses the native dotnet TCP classes, which do not implement interfaces. This means that in order to mock them in a unit test,
 * we would need to create wrapper classes + interfaces for all these TCP classes. Even then, there is very little logic here
 * that would meaningfully benefit from unit tests. High effort (or at least cumbersome effort), very low value.
 */
[ExcludeFromCodeCoverage]
public class TcpHealthProbeService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly TcpListener _listener;
    private readonly ILogger<TcpHealthProbeService> _logger;
    private readonly HealthChecksConfig _healthChecksConfig;

    public TcpHealthProbeService(
        ILogger<TcpHealthProbeService> logger,
        HealthCheckService healthCheckService,
        HealthChecksConfig healthChecksConfig)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _healthChecksConfig = healthChecksConfig;
        _listener = new TcpListener(
            IPAddress.Any,
            healthChecksConfig.HealthCheckProbeTcpPort);
    }

    protected virtual async Task<bool> CustomHealthCheckAsync()
    {
        await Task.CompletedTask;

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _listener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateHeartbeatAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(_healthChecksConfig.HealthCheckProbePollFrequencySeconds), stoppingToken);
        }

        _listener.Stop();
    }

    private async Task UpdateHeartbeatAsync(CancellationToken stoppingToken)
    {
        try
        {
            var result = await _healthCheckService.CheckHealthAsync(stoppingToken);
            var isHealthy = result.Status == HealthStatus.Healthy;

            if (isHealthy)
            {
                isHealthy = await CustomHealthCheckAsync();
            }

            if (!isHealthy)
            {
                _listener.Stop();
                _logger.LogInformation("Service is unhealthy, listener stopped");
                return;
            }

            _listener.Start();
            while (_listener.Server.IsBound && _listener.Pending())
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                client.Close();
                _logger.LogDebug("Service is healthy, listener connected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ErrorCodes.TcpHealthProbeFailed, ex, "An error occurred while checking heartbeat");
        }
    }

    public override void Dispose()
    {
        _listener.Stop();
        _listener.Dispose();
        base.Dispose();
    }
}
