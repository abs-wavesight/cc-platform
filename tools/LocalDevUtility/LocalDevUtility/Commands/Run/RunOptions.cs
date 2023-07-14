// ReSharper disable UnusedMember.Global

using Abs.CommonCore.LocalDevUtility.Extensions;

namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

// ReSharper disable once ClassNeverInstantiated.Global
public class RunOptions
{
    [RunComponent(composePath: "drex-service", imageName: "cc-drex-service")]
    [RunComponentDependency(nameof(RabbitmqLocal))]
    [RunComponentDependency(nameof(Vector))]
    public RunComponentMode? DrexService { get; set; }

    [RunComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqLocal)]
    public RunComponentMode? RabbitmqLocal { get; set; }

    [RunComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqRemote)]
    public RunComponentMode? RabbitmqRemote { get; set; }

    [RunComponent(composePath: "vector", imageName: "vector", defaultVariant: "default")]
    [RunComponentDependency(nameof(RabbitmqLocal))]
    public RunComponentMode? Vector { get; set; }

    [RunComponent(composePath: "grafana", imageName: "grafana")]
    [RunComponentDependency(nameof(Vector))]
    [RunComponentDependency(nameof(Loki))]
    public RunComponentMode? Grafana { get; set; }

    // [RunComponent(composePath: "loki", imageName: "loki", dependencyPropertyNames: new []{nameof(Vector)})]
    [RunComponent(composePath: "loki", imageName: "loki")]
    [RunComponentDependency(nameof(Vector))]
    public RunComponentMode? Loki { get; set; }


    [RunComponent(aliasPropertyNames: nameof(RabbitmqLocal))]
    public RunComponentMode? Rabbitmq { get; set; }

    [RunComponent(aliasPropertyNames: new []{nameof(RabbitmqLocal), nameof(RabbitmqRemote), nameof(Vector)})]
    public RunComponentMode? Deps { get; set; }

    [RunComponent(aliasPropertyNames: new []{nameof(Grafana), nameof(Loki)})]
    public RunComponentMode? LogViz { get; set; }

    public RunMode? Mode { get; set; }
    public bool? Reset { get; set; }
    public bool? Background { get; set; }
    public bool? AbortOnContainerExit { get; set; }
    public string? DrexSiteConfigFileNameOverride { get; set; }

    public static List<string> ComponentPropertyNames => typeof(RunOptions)
        .GetProperties()
        .Where(_ => _.PropertyType == typeof(RunComponentMode?) || _.PropertyType == typeof(RunComponentMode))
        .Select(_ => _.Name)
        .ToList();

    public static List<string> NonAliasComponentPropertyNames => ComponentPropertyNames
        .Where(_ => (typeof(RunOptions).GetRunComponent(_)?.IsAlias ?? false) == false)
        .ToList();

    public static List<string> AliasComponentPropertyNames => ComponentPropertyNames
        .Where(_ => typeof(RunOptions).GetRunComponent(_)?.IsAlias == true)
        .ToList();
}
