using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;

public class RabbitMqVhostModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
