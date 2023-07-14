namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ComposeComponentAttribute : Attribute
{
    public ComposeComponentAttribute(
        string composePath,
        string imageName,
        string? profile = null,
        string? defaultVariant = null)
    {
        ComposePath = composePath;
        ImageName = imageName;
        Profile = profile;
        DefaultVariant = defaultVariant;
    }

    public string? ComposePath { get; }
    public string? ImageName { get; }
    public string? Profile { get; }
    public string? DefaultVariant { get; }
}
