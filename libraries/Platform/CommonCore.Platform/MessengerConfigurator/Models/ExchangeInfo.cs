namespace Abs.CommonCore.Platform.MessengerConfigurator.Models;

/// <summary>
/// Entity that distributes messages. Examples: Exchange in RabbitMQ, Topic in Azure Service Bus
/// </summary>
public class ExchangeInfo
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Vhost { get; init; } = "/";
    public bool AutoDelete { get; init; } = false;
    public bool Durable { get; init; } = true;
    public bool Internal { get; init; } = false;
    public List<QueueInfo> Queues { get; init; } = new List<QueueInfo>();
}
