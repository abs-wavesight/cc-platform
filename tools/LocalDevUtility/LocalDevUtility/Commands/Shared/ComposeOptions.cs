// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Abs.CommonCore.LocalDevUtility.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Commands.Run.Attributes;
using Abs.CommonCore.LocalDevUtility.Extensions;

namespace Abs.CommonCore.LocalDevUtility.Commands.Shared;

public abstract class ComposeOptions
{
    [RunComponent(composePath: "drex-service", imageName: "cc-drex-service")]
    [RunComponentDependency(nameof(RabbitmqLocal))]
    [RunComponentDependency(nameof(Vector))]
    public RunComponentMode? DrexService { get; set; }

    [RunComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqLocal)]
    public RunComponentMode? RabbitmqLocal { get; set; }

    [Description("Run a copy of rabbitmq with a different hostname and port to represent a remote instance (e.g. Central)")]
    [RunComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqRemote)]
    public RunComponentMode? RabbitmqRemote { get; set; }

    [RunComponent(composePath: "vector", imageName: "vector", defaultVariant: "default")]
    [RunComponentDependency(nameof(RabbitmqLocal))]
    public RunComponentMode? Vector { get; set; }

    [RunComponent(composePath: "grafana", imageName: "grafana")]
    [RunComponentDependency(nameof(Vector))]
    [RunComponentDependency(nameof(Loki))]
    public RunComponentMode? Grafana { get; set; }

    [RunComponent(composePath: "loki", imageName: "loki")]
    [RunComponentDependency(dependencyPropertyName: nameof(Vector), variant: "loki")]
    public RunComponentMode? Loki { get; set; }


    [Description("Alias for \"rabbitmq-local\"")]
    [RunComponentAlias(nameof(RabbitmqLocal))]
    public RunComponentMode? Rabbitmq { get; set; }

    [Description("Alias for \"rabbitmq\", \"rabbitmq-remote\", and \"vector\"")]
    [RunComponentAlias(nameof(RabbitmqLocal))]
    [RunComponentAlias(nameof(RabbitmqRemote))]
    [RunComponentAlias(nameof(Vector))]
    public RunComponentMode? Deps { get; set; }

    [Description("Alias for \"loki\" and \"grafana\"")]
    [RunComponentAlias(nameof(Grafana))]
    [RunComponentAlias(nameof(Loki))]
    public RunComponentMode? LogViz { get; set; }

    public static List<string> ComponentPropertyNames => typeof(RunOptions)
        .GetProperties()
        .Where(_ => _.PropertyType == typeof(RunComponentMode?) || _.PropertyType == typeof(RunComponentMode))
        .Select(_ => _.Name)
        .ToList();

    public static List<string> NonAliasComponentPropertyNames => ComponentPropertyNames
        .Where(_ => !typeof(RunOptions).GetRunComponentAliases(_).Any())
        .ToList();

    public static List<string> AliasComponentPropertyNames => ComponentPropertyNames
        .Where(_ => typeof(RunOptions).GetRunComponentAliases(_).Any())
        .ToList();
}
