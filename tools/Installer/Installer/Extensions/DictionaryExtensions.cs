namespace Abs.CommonCore.Installer.Extensions;

public static class DictionaryExtensions
{
    private const string WindowsVersionKey = "$WINDOWS_VERSION";

    public static Dictionary<string, string> MergeParameters(this Dictionary<string, string> source, Dictionary<string, string>? parameters)
    {
        if (parameters == null)
        {
            return source;
        }

        foreach (var parameter in parameters)
        {
            source[parameter.Key] = parameter.Value;
        }

        return source;
    }

    public static string GetWindowsVersion(this Dictionary<string, string> parameters)
    {
        var isFound = parameters.TryGetValue(WindowsVersionKey, out var value);

        return isFound && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Windows version not provided as a parameter");
    }
}
