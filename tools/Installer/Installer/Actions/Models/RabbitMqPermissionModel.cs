using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;

public class RabbitMqPermissionModel
{
    [JsonPropertyName("vhost")]
    public string VHost { get; set; } = string.Empty;
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;
    [JsonPropertyName("configure")]
    public string Configure { get; set; } = string.Empty;
    [JsonPropertyName("write")]
    public string Write { get; set; } = string.Empty;
    [JsonPropertyName("read")]
    public string Read { get; set; } = string.Empty;
}
