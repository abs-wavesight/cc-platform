using System.Net.Sockets;
using Abs.CommonCore.Platform.Config;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Platform.Extensions.Hosting;

public class TcpHealthProbeService(
    ILogger<TcpHealthProbeService> logger,
    HealthCheckService healthCheckService,
    HealthChecksConfig healthChecksConfig)
    : BackgroundService
{
    private readonly TcpListener _listener = new(
        healthChecksConfig.HealthCheckProbeIpAddress,
        healthChecksConfig.HealthCheckProbeTcpPort);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _listener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateHeartbeatAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(healthChecksConfig.HealthCheckProbePollFrequencySeconds), stoppingToken);
        }

        _listener.Stop();
    }

    private async Task UpdateHeartbeatAsync(CancellationToken stoppingToken)
    {
        try
        {
            var result = await healthCheckService.CheckHealthAsync(stoppingToken);
            var isHealthy = result.Status == HealthStatus.Healthy;
            if (!isHealthy)
            {
                _listener.Stop();
                logger.LogInformation("Service is unhealthy, listener stopped");
                return;
            }

            _listener.Start();
            while (_listener.Server.IsBound && _listener.Pending())
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                client.Close();
                logger.LogDebug("Service is healthy, listener connected");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while checking heartbeat");
        }
    }

    public override void Dispose()
    {
        _listener.Dispose();
        base.Dispose();
    }
}
