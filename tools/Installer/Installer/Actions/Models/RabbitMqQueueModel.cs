using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;

public class RabbitMqQueueModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("vhost")]
    public string VHost { get; set; } = string.Empty;
    [JsonPropertyName("durable")]
    public bool Durable { get; set; }
    [JsonPropertyName("auto_delete")]
    public bool AutoDelete { get; set; }
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}
