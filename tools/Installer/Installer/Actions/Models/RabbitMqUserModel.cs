using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;

public class RabbitMqUserModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }
}
