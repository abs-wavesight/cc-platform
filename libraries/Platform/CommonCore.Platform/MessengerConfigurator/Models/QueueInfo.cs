namespace Abs.CommonCore.Platform.MessengerConfigurator.Models
{
    /// <summary>
    /// Entity that delivers messages to consumer. Examples: Queue in RabbitMQ, Subscription in Azure Service Bus
    /// </summary>
    public class QueueInfo
    {
        public string VHost { get; init; } = "";
        public string Name { get; init; } = "";
        public bool AutoDelete { get; init; } = false;
        public bool Durable { get; init; } = true;
    }
}
