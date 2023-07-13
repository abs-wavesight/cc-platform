namespace Abs.CommonCore.LocalDevUtility.Models;

public class RunComponentAttribute : Attribute
{
    public RunComponentAttribute(string composePath, string imageName, string? profile = null, params string[] dependencyPropertyNames)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DependencyPropertyNames = dependencyPropertyNames;
    }

    public string ComposePath { get; }
    public string ImageName { get; }
    public string? Profile { get; }
    public string[] DependencyPropertyNames { get; }
}
