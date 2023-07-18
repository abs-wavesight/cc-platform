using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Config
{
    public class Component
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("files")]
        public ComponentFile[] Files { get; init; } = Array.Empty<ComponentFile>();

        [JsonPropertyName("actions")]
        public ComponentAction[] Actions { get; init; } = Array.Empty<ComponentAction>();
    }
}
