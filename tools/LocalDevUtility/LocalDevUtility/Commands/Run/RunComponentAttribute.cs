using Microsoft.Extensions.Logging.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

[AttributeUsage(AttributeTargets.Property)]
public class RunComponentAttribute : Attribute
{
    public RunComponentAttribute(
        string composePath,
        string imageName,
        string? profile = null,
        string? defaultVariant = null)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DefaultVariant = defaultVariant;
        AliasPropertyNames = Array.Empty<string>();
    }

    public RunComponentAttribute(params string[] aliasPropertyNames)
    {
        AliasPropertyNames = aliasPropertyNames;
        ComposePath = null;
        ImageName = null;
        Profile = null;
        DefaultVariant = null;
    }

    public string[] AliasPropertyNames { get; }
    public bool IsAlias => AliasPropertyNames.Any();
    public string? ComposePath { get; }
    public string? ImageName { get; }
    public string? Profile { get; }
    public string? DefaultVariant { get; }
}

// TODO RH: Add new attribute for "RunComponentAlias"

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
