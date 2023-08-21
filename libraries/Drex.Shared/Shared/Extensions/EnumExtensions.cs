using System.ComponentModel;

namespace Abs.CommonCore.Drex.Shared.Extensions;
public static class EnumExtensions
{
    /// <summary>
    /// Gets <see cref="DescriptionAttribute.Description"/> value.
    /// </summary>
    /// <typeparam name="T">Type of the enum.</typeparam>
    /// <param name="enumerationValue">The enum value.</param>
    /// <returns>
    /// <see cref="DescriptionAttribute.Description"/> value.
    /// If <see cref="DescriptionAttribute"/> not found, returns string representation of the enum.
    /// </returns>
    public static string GetDescription<T>(this T enumerationValue)
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

        return enumString;
    }
}
