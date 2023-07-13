using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Downloader.Config
{
    public class ComponentFile
    {
        [JsonPropertyName("type")]
        public FileType Type { get; init; }

        [JsonPropertyName("source")]
        public string Source { get; init; } = "";

        [JsonPropertyName("destination")]
        public string Destination { get; init; } = "";
    }
}
