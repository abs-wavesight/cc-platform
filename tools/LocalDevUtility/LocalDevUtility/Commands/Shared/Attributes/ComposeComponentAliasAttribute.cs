namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

/// <summary>
/// Alias for a different Compose component (service)
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ComposeComponentAliasAttribute : Attribute
{
    /// <summary>
    /// Property name of the component that this is an alias for
    /// </summary>
    public string AliasPropertyName { get; }

    public ComposeComponentAliasAttribute(string aliasPropertyName)
    {
        AliasPropertyName = aliasPropertyName;
    }
}
