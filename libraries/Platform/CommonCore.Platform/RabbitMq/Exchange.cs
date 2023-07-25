using EasyNetQ.Management.Client.Model;

namespace Abs.CommonCore.Platform.RabbitMq
{
    public class Exchange
    {
        public ExchangeInfo Info { get; set; }
        public List<QueueInfo> Queues { get; set; }
    }
}
