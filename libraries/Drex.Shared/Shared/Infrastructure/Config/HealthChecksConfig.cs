using System.Diagnostics.CodeAnalysis;

namespace Abs.CommonCore.Drex.Shared.Infrastructure.Config;

[ExcludeFromCodeCoverage]
public class HealthChecksConfig
{
    public const string Key = "HealthChecks";
    public int HealthCheckProbePollFrequencySeconds { get; init; } = 30;
    public int HealthCheckProbeTcpPort { get; init; } = 5000;
}
