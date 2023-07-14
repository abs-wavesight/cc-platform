namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

/// <summary>
/// Dependency component (service) for the owning component, which must be started if the owning component is started
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ComposeComponentDependencyAttribute : Attribute
{
    /// <summary>
    /// Sibling property name of the component dependency
    /// </summary>
    public string DependencyPropertyName { get; }

    /// <summary>
    /// Optional compose file variant to use when this dependent component is started (`docker-compose.variant.{value}.yml`)
    /// </summary>
    public string? Variant { get; }

    public ComposeComponentDependencyAttribute(string dependencyPropertyName, string? variant = null)
    {
        DependencyPropertyName = dependencyPropertyName;
        Variant = variant;
    }
}
