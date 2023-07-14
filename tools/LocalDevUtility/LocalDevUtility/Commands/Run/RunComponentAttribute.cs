namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

public class RunComponentAttribute : Attribute
{
    public RunComponentAttribute(string composePath, string imageName, string? profile = null, params string[] dependencyPropertyNames)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DependencyPropertyNames = dependencyPropertyNames;
        AliasPropertyNames = Array.Empty<string>();
    }

    public RunComponentAttribute(params string[] aliasPropertyNames)
    {
        AliasPropertyNames = aliasPropertyNames;
        ComposePath = null;
        ImageName = null;
        Profile = null;
        DependencyPropertyNames = Array.Empty<string>();
    }

    public string[] AliasPropertyNames { get; }
    public bool IsAlias => AliasPropertyNames.Any();
    public string? ComposePath { get; }
    public string? ImageName { get; }
    public string? Profile { get; }
    public string[] DependencyPropertyNames { get; }
}
