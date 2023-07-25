using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Platform.RabbitMq
{
    public class RabbitConfigurer
    {
        private readonly ILogger _logger;

        public RabbitConfigurer(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CreateExchangesWithQueuesAsync(string busKey, IManagementClient managementClient, string vhost, List<Exchange> exchanges, CancellationToken cancellationToken = default)
        {
            foreach (var exchange in exchanges)
            {
                await CreateExchangeAsync(busKey, managementClient, vhost, exchange.Info, cancellationToken);
                foreach (var queue in exchange.Queues)
                {
                    await CreateQueueAsync(busKey, managementClient, vhost, queue, cancellationToken);
                    await CreateQueueBindingAsync(busKey, managementClient, vhost, exchange.Info.Name, queue.Name, new BindingInfo(queue.Name), cancellationToken);
                }
            }
        }

        public async Task CreateExchangeAsync(string busKey, IManagementClient managementClient, string vhostName, ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            await managementClient.CreateExchangeAsync(vhostName, exchangeInfo, cancellationToken);
            LogResourceCreation(busKey, "Exchange", exchangeInfo.Name);
        }

        public async Task CreateQueueAsync(string busKey, IManagementClient managementClient, string vhostName, QueueInfo queueInfo, CancellationToken cancellationToken = default)
        {
            await managementClient.CreateQueueAsync(vhostName, queueInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue", queueInfo.Name);
        }

        public async Task CreateQueueBindingAsync(string busKey, IManagementClient managementClient, string vhostName, string exchangeName, string queueName, BindingInfo bindingInfo, CancellationToken cancellationToken = default)
        {
            await managementClient.CreateQueueBindingAsync(vhostName, exchangeName, queueName, bindingInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue Binding", $"{exchangeName} -> {queueName}");
        }

        public void LogResourceCreation(string busKey, string resourceType, string resourceName)
        {
            _logger.LogInformation($"Created {busKey} RabbitMQ {resourceType}: {resourceName}");
        }
    }
}
