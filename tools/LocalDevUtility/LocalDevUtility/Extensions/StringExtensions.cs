namespace Abs.CommonCore.LocalDevUtility.Extensions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
    }

    public static string TrimTrailingSlash(this string str)
    {
        var trimmed = str.Trim();
        return trimmed[^1] is '/' or '\\'
            ? trimmed[..^1]
            : trimmed;
    }

    public static string ToForwardSlashes(this string str)
    {
        return str.Replace("\\", "/");
    }
}
