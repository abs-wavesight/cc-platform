using System.ComponentModel;
using System.Reflection;

namespace Abs.CommonCore.Platform.Extensions;

public static class AttributeExtensions
{
    public static string? GetDescription(this Type type, string propertyName)
    {
        return type
            .GetProperty(propertyName)
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description;
    }

    /// <summary>
    /// Gets <see cref="DescriptionAttribute.Description"/> value.
    /// </summary>
    /// <typeparam name="T">Type of the enum.</typeparam>
    /// <param name="enumerationValue">The enum value.</param>
    /// <returns>
    /// <see cref="DescriptionAttribute.Description"/> value.
    /// If <see cref="DescriptionAttribute"/> not found, returns null.
    /// </returns>
    public static string? GetDescription<T>(this T enumerationValue)
        where T : Enum
    {
        var type = enumerationValue.GetType();
        var enumString = enumerationValue.ToString();
        var memberInfo = type.GetMember(enumString);
        if (memberInfo.Length > 0)
        {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        return null;
    }
}
