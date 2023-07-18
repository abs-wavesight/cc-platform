using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Config
{
    public class ComponentAction
    {
        [JsonPropertyName("source")]
        public string Source { get; init; } = "";

        [JsonPropertyName("destination")]
        public string Destination { get; init; } = "";

        [JsonPropertyName("action")]
        public ActionType Action { get; init; }
    }
}
