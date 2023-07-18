namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

/// <summary>
/// Represents a component (service) that will be controlled via Docker Compose
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ComposeComponentAttribute : Attribute
{
    /// <summary>
    /// Path to compose files, relative to the compose/local-dev directory
    /// </summary>
    public string? ComposePath { get; }

    /// <summary>
    /// Container image name to use, used for pulling prior to running Compose commands
    /// </summary>
    public string? ImageName { get; }

    /// <summary>
    /// Optional profile to set for this component (--profile Compose parameter)
    /// </summary>
    public string? Profile { get; }

    /// <summary>
    /// Default variant compose file to add in when component is used (`docker-compose.variant.{value}.yml`)
    /// </summary>
    public string? DefaultVariant { get; }

    /// <summary>
    /// </summary>
    /// <param name="composePath">Path to compose files, relative to the compose/local-dev directory</param>
    /// <param name="imageName">Container image name to use, used for pulling prior to running Compose commands</param>
    /// <param name="profile">Optional profile to set for this component (--profile Compose parameter)</param>
    /// <param name="defaultVariant">Optional default variant compose file to add in when component is used (`docker-compose.variant.{value}.yml`)</param>
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
}
