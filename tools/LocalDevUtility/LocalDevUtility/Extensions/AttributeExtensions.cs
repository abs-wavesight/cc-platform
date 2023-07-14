using System.Reflection;
using Abs.CommonCore.LocalDevUtility.Commands.Run;

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
}
