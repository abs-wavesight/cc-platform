using System.Reflection;
using Abs.CommonCore.LocalDevUtility.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Commands.Run.Attributes;

namespace Abs.CommonCore.LocalDevUtility.Extensions;

public static class AttributeExtensions
{
    public static RunComponentAttribute? GetRunComponent(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return (object?) property == null
            ? null
            : property.GetCustomAttribute<RunComponentAttribute>();
    }

    public static IEnumerable<RunComponentAliasAttribute> GetRunComponentAliases(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return (object?) property == null
            ? Array.Empty<RunComponentAliasAttribute>()
            : property.GetCustomAttributes<RunComponentAliasAttribute>();
    }

    public static IEnumerable<RunComponentDependencyAttribute> GetRunComponentDependencies(this Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return (object?) property == null
            ? Array.Empty<RunComponentDependencyAttribute>()
            : property.GetCustomAttributes<RunComponentDependencyAttribute>();
    }
}
