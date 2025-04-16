using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;
public class RabbitDefinitionModel
{
    [JsonPropertyName("vhosts")]
    public RabbitMqVhostModel[] VHosts { get; set; } = Array.Empty<RabbitMqVhostModel>();
    [JsonPropertyName("users")]
    public RabbitMqUserModel[] Users { get; set; } = Array.Empty<RabbitMqUserModel>();
    [JsonPropertyName("permissions")]
    public RabbitMqPermissionModel[] Permissions { get; set; } = Array.Empty<RabbitMqPermissionModel>();
    [JsonPropertyName("queues")]
    public RabbitMqQueueModel[] Queues { get; set; } = Array.Empty<RabbitMqQueueModel>();
}
