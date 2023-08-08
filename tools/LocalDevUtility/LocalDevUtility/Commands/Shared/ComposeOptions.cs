// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Abs.CommonCore.LocalDevUtility.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;
using Abs.CommonCore.LocalDevUtility.Extensions;

namespace Abs.CommonCore.LocalDevUtility.Commands.Shared;

public abstract class ComposeOptions
{
    [ComposeComponent(composePath: "openssl", imageName: "openssl")]
    public ComposeComponentMode? Openssl { get; set; }

    [ComposeComponent(composePath: "drex-message-service", imageName: "cc-drex-service")]
    [ComposeComponentDependency(nameof(RabbitmqLocal))]
    [ComposeComponentDependency(nameof(VectorSite))]
    public ComposeComponentMode? DrexMessageService { get; set; }

    [ComposeComponent(composePath: "drex-file-service", imageName: "cc-drex-file-service")]
    [ComposeComponentDependency(nameof(RabbitmqLocal))]
    [ComposeComponentDependency(nameof(VectorSite))]
    [ComposeComponentDependency(nameof(DrexMessageService))]
    public ComposeComponentMode? DrexFileService { get; set; }

    [ComposeComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqLocal)]
    public ComposeComponentMode? RabbitmqLocal { get; set; }

    [Description("Run a copy of rabbitmq with a different hostname and port to represent a remote instance (e.g. Central)")]
    [ComposeComponent(composePath: "rabbitmq", imageName: "rabbitmq", profile: Constants.Profiles.RabbitMqRemote)]
    public ComposeComponentMode? RabbitmqRemote { get; set; }

    [ComposeComponent(composePath: "vector", imageName: "vector", profile: Constants.Profiles.VectorSite, defaultVariant: "default")]
    [ComposeComponentDependency(nameof(RabbitmqLocal))]
    public ComposeComponentMode? VectorSite { get; set; }

    [ComposeComponent(composePath: "vector", imageName: "vector", profile: Constants.Profiles.VectorCentral, defaultVariant: "default")]
    [ComposeComponentDependency(nameof(RabbitmqRemote))]
    public ComposeComponentMode? VectorCentral { get; set; }

    [ComposeComponent(composePath: "grafana", imageName: "grafana")]
    [ComposeComponentDependency(nameof(VectorSite))]
    [ComposeComponentDependency(nameof(Loki))]
    public ComposeComponentMode? Grafana { get; set; }

    [ComposeComponent(composePath: "loki", imageName: "loki")]
    [ComposeComponentDependency(dependencyPropertyName: nameof(VectorSite), variant: "loki")]
    public ComposeComponentMode? Loki { get; set; }


    [Description("Alias for \"rabbitmq-local\"")]
    [ComposeComponentAlias(nameof(RabbitmqLocal))]
    public ComposeComponentMode? Rabbitmq { get; set; }

    [Description("Alias for \"vector-site\"")]
    [ComposeComponentAlias(nameof(VectorSite))]
    public ComposeComponentMode? Vector { get; set; }

    [Description("Alias for \"rabbitmq\", \"rabbitmq-remote\", and \"vector\"")]
    [ComposeComponentAlias(nameof(RabbitmqLocal))]
    [ComposeComponentAlias(nameof(RabbitmqRemote))]
    [ComposeComponentAlias(nameof(VectorSite))]
    [ComposeComponentAlias(nameof(VectorCentral))]
    public ComposeComponentMode? Deps { get; set; }

    [Description("Alias for \"loki\" and \"grafana\"")]
    [ComposeComponentAlias(nameof(Grafana))]
    [ComposeComponentAlias(nameof(Loki))]
    public ComposeComponentMode? LogViz { get; set; }

    public static List<string> ComponentPropertyNames => typeof(RunOptions)
        .GetProperties()
        .Where(i => i.PropertyType == typeof(ComposeComponentMode?) || i.PropertyType == typeof(ComposeComponentMode))
        .Select(i => i.Name)
        .ToList();

    public static List<string> NonAliasComponentPropertyNames => ComponentPropertyNames
        .Where(s => !typeof(RunOptions).GetRunComponentAliases(s).Any())
        .ToList();

    public static List<string> AliasComponentPropertyNames => ComponentPropertyNames
        .Where(s => typeof(RunOptions).GetRunComponentAliases(s).Any())
        .ToList();
}
