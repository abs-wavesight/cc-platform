using System.Net;

namespace Abs.CommonCore.Platform.Config;

public class HealthChecksConfig
{
    public int HealthCheckProbePollFrequencySeconds { get; init; } = 30;
    public int HealthCheckProbeTcpPort { get; init; } = 5000;
    public IPAddress HealthCheckProbeIpAddress { get; init; } = IPAddress.Any;
}
