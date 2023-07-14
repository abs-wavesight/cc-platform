namespace Abs.CommonCore.LocalDevUtility.Commands.Run.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RunComponentAliasAttribute : Attribute
{
    public string AliasPropertyName { get; }

    public RunComponentAliasAttribute(string aliasPropertyName)
    {
        AliasPropertyName = aliasPropertyName;
    }
}
