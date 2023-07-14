namespace Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ComposeComponentAliasAttribute : Attribute
{
    public string AliasPropertyName { get; }

    public ComposeComponentAliasAttribute(string aliasPropertyName)
    {
        AliasPropertyName = aliasPropertyName;
    }
}
