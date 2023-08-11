

namespace Abs.CommonCore.Installer.Extensions
{
    public static class StringExtensions
    {
        public static (string Owner, string Repo, string Tag, string? File) GetGitHubPathSegments(this string source)
        {
            // "https://github.com/abs-wavesight/cc-platform/releases/download/$RABBIT_RELEASE_TAG/"
            var uri = new Uri(source, UriKind.Absolute);
            var segments = uri.Segments
                .Select(x => x.Trim('/'))
                .Where(x => x.Length > 0)
                .ToArray();

            var file = segments.Length > 5
                ? segments[5]
                : null;

            return (segments[0], segments[1], segments[4], file);
        }

        public static string ReplaceConfigParameters(this string text, Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                text = text.Replace(parameter.Key, parameter.Value, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}
