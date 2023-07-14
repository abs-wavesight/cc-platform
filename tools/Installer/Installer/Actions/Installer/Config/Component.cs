using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Installer.Config
{
    public class Component
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("actions")]
        public ComponentAction[] Actions { get; init; } = Array.Empty<ComponentAction>();
    }
}
