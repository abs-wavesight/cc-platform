namespace Abs.CommonCore.LocalDevUtility.Commands.Run.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RunComponentDependencyAttribute : Attribute
{
    public string DependencyPropertyName { get; }
    public string? Variant { get; }

    public RunComponentDependencyAttribute(string dependencyPropertyName, string? variant = null)
    {
        DependencyPropertyName = dependencyPropertyName;
        Variant = variant;
    }
}