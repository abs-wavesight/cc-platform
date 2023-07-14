using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Installer.Config
{
    public class ComponentAction
    {
        [JsonPropertyName("source")]
        public string[] Source { get; init; } = Array.Empty<string>();

        [JsonPropertyName("action")]
        public ActionType Action { get; init; }
    }
}
