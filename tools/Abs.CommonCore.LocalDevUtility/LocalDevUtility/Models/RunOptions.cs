// ReSharper disable UnusedMember.Global
namespace Abs.CommonCore.LocalDevUtility.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class RunOptions
{
    [RunComponent(composePath: "drex-service", imageName: "cc-drex-service", dependencyPropertyNames: nameof(Rabbitmq))]
    public RunComponentMode? DrexService { get; set; }

    [RunComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqLocal)]
    public RunComponentMode? Rabbitmq { get; set; }

    [RunComponent(composePath: "vector", imageName: "vector", dependencyPropertyNames: nameof(Rabbitmq))]
    public RunComponentMode? Vector { get; set; }

    [RunComponent(composePath: "grafana", imageName: "grafana")]
    public RunComponentMode? Grafana { get; set; }

    [RunComponent(composePath: "loki", imageName: "loki")]
    public RunComponentMode? Loki { get; set; }

    public bool? Deps { get; set; }
    public bool? LogViz { get; set; }
    public bool? Reset { get; set; }
    public bool? Background { get; set; }
    public bool? AbortOnContainerExit { get; set; }
    public bool? Confirm { get; set; }
    public string? DrexSiteConfigFileNameOverride { get; set; }

    public static List<string> ComponentPropertyNames => typeof(RunOptions)
        .GetProperties()
        .Where(_ => _.PropertyType == typeof(RunComponentMode?) || _.PropertyType == typeof(RunComponentMode))
        .Select(_ => _.Name)
        .ToList();
}
