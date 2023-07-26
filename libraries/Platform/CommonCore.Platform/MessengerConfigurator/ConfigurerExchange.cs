namespace Abs.CommonCore.Platform.MessengerConfigurator
{
    /// <summary>
    /// Entity that distributes messages. Examples: Exchnge in RabbitMQ, Topic in Azure Service Bus
    /// </summary>
    public class ConfigurerExchange
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Vhost { get; set; } = "/";
        public bool AutoDelete { get; set; } = false;
        public bool Durable { get; set; } = true;
        public bool Internal { get; set; } = false;
        public List<ConfigurerQueue> Deliverymen { get; set; } = new List<ConfigurerQueue>();
    }
}
