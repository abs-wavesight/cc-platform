using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Downloader.Config
{
    public class Component
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("files")]
        public ComponentFile[] Files { get; init; } = Array.Empty<ComponentFile>();
    }
}
