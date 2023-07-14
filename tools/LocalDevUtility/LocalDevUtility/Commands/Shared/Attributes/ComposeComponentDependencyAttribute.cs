namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ComposeComponentDependencyAttribute : Attribute
{
    public string DependencyPropertyName { get; }
    public string? Variant { get; }

    public ComposeComponentDependencyAttribute(string dependencyPropertyName, string? variant = null)
    {
        DependencyPropertyName = dependencyPropertyName;
        Variant = variant;
    }
}