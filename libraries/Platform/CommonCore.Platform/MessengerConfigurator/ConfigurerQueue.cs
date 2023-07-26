namespace Abs.CommonCore.Platform.MessengerConfigurator
{
    /// <summary>
    /// Entity that delivers messages to cunsumer. Examples: Queue in RabbitMQ, Subscription in Azure Service Bus
    /// </summary>
    public class ConfigurerQueue
    {
        public string Name { get; set; } = "";

        public bool AutoDelete { get; set; } = false;

        public bool Durable { get; set; } = true;
    }
}
