namespace Abs.CommonCore.Installer.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<string, string> MergeParameters(this Dictionary<string, string> source, Dictionary<string, string>? parameters)
    {
        if (parameters == null) return source;

        foreach (var parameter in parameters)
        {
            source[parameter.Key] = parameter.Value;
        }

        return source;
    }
}
