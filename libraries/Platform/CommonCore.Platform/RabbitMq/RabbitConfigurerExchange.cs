using EasyNetQ.Management.Client.Model;

namespace Abs.CommonCore.Platform.RabbitMq
{
    public class RabbitConfigurerExchange
    {
        public RabbitConfigurerExchange(string name, string type)
        {
            Info = new ExchangeInfo(name, type);
        }

        public ExchangeInfo Info { get; set; }
        public string Vhost { get; set; } = "/";
        public List<QueueInfo> Queues { get; set; } = new List<QueueInfo>();
    }
}
