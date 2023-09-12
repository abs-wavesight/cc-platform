using System.Reflection;
using Abs.CommonCore.LocalDevUtility.Commands.Shared.Attributes;

namespace Abs.CommonCore.LocalDevUtility.Extensions;

public static class AttributeExtensions
{
    public static ComposeComponentAttribute? GetRunComponent(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return property?.GetCustomAttribute<ComposeComponentAttribute>();
    }

    public static IEnumerable<ComposeComponentAliasAttribute> GetRunComponentAliases(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return property is null
            ? Array.Empty<ComposeComponentAliasAttribute>()
            : property.GetCustomAttributes<ComposeComponentAliasAttribute>();
    }

    public static IEnumerable<ComposeComponentDependencyAttribute> GetRunComponentDependencies(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return property is null
            ? Array.Empty<ComposeComponentDependencyAttribute>()
            : property.GetCustomAttributes<ComposeComponentDependencyAttribute>();
    }
}
