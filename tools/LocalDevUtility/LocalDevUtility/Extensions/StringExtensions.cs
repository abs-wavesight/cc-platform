namespace Abs.CommonCore.LocalDevUtility.Extensions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
    }

    public static string TrimTrailingSlash(this string str)
    {
        return str.Trim().TrimEnd('\\').TrimEnd('/');
    }

    public static string ToForwardSlashes(this string str)
    {
        return str.Replace("\\", "/");
    }
}
