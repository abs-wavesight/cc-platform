using System.ComponentModel;
using System.Reflection;

namespace Abs.CommonCore.Drex.Shared.Extensions
{
    public static class AttributeExtensions
    {
        public static string? GetDescription(this Type type, string propertyName)
        {
            return type
                .GetProperty(propertyName)
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description;
        }
    }
}
