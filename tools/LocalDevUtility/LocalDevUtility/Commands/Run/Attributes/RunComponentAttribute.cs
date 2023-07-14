// TODO RH: Move drex config back to drex repo; use absolute path in docker-compose
namespace Abs.CommonCore.LocalDevUtility.Commands.Run.Attributes;

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
    }

    public string? ComposePath { get; }
    public string? ImageName { get; }
    public string? Profile { get; }
    public string? DefaultVariant { get; }
}
