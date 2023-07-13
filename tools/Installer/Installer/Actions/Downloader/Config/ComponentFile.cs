using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Downloader.Config
{
    public class ComponentFile
    {
        [JsonPropertyName("source")]
        public string Source { get; init; }

        [JsonPropertyName("destination")]
        public string Destination { get; init; }
    }
}
