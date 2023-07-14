using Microsoft.Extensions.Logging.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

public class RunComponentAttribute : Attribute
{
    public RunComponentAttribute(
        string composePath,
        string imageName,
        string? profile = null,
        string? defaultVariant = null,
        string[]? dependencyPropertyNames = null)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DefaultVariant = defaultVariant;
        DependencyPropertyNames = dependencyPropertyNames ?? Array.Empty<string>();
        AliasPropertyNames = Array.Empty<string>();
    }

    public RunComponentAttribute(
        string composePath,
        string imageName,
        Dictionary<string,string?>? variantsByDependencyPropertyName,
        string? profile = null,
        string? defaultVariant = null)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DefaultVariant = defaultVariant;
        DependencyPropertyNames = Array.Empty<string>();
        VariantsByDependencyPropertyName = variantsByDependencyPropertyName ?? new Dictionary<string, string?>();
        AliasPropertyNames = Array.Empty<string>();
    }

    public RunComponentAttribute(params string[] aliasPropertyNames)
    {
        AliasPropertyNames = aliasPropertyNames;
        ComposePath = null;
        ImageName = null;
        Profile = null;
        DefaultVariant = null;
        DependencyPropertyNames = Array.Empty<string>();
    }

    public string[] AliasPropertyNames { get; }
    public bool IsAlias => AliasPropertyNames.Any();
    public string? ComposePath { get; }
    public string? ImageName { get; }
    public string? Profile { get; }
    public string? DefaultVariant { get; }
    public Dictionary<string,string?>? VariantsByDependencyPropertyName { get; }
    public string[] DependencyPropertyNames { get; }
}
