using Abs.CommonCore.Contracts.Json;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Platform.MessengerConfigurator
{
    public class RabbitMqConfigurer : IMessengerConfigurator
    {
        private readonly ILogger _logger;
        private const string _queue = "Queue";
        private const string _exchange = "Exchange";
        private readonly IManagementClient _managementClient;

        public RabbitMqConfigurer(ILogger logger, BusConnection busConnection, TimeSpan timeout)
        {
            _logger = logger;
            var address = new Uri($"http://{busConnection.Host}:{busConnection.HttpPort}");
            _managementClient = new ManagementClient(address,
                busConnection.Username,
                busConnection.Password,
                timeout: timeout);
        }

        public async Task BindDistributorAndDeliverymenAsync(string busKey, string distributorName, string deliverymanName, string bindingName = "", string vhostName = "/", CancellationToken cancellationToken = default)
        {
            var bindingInfo = new BindingInfo(bindingName);
            await _managementClient.CreateQueueBindingAsync(vhostName, distributorName, deliverymanName, bindingInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue Binding", $"{distributorName} -> {deliverymanName}");
        }

        public async Task CreateDeliverymanAsync(string busKey, MessageDeliveryman deliveryman, string vhost = "/", CancellationToken cancellationToken = default)
        {
            var queueInfo = DeliverymanToQueueInfo(deliveryman);
            await _managementClient.CreateQueueAsync(vhost, queueInfo, cancellationToken);
            LogResourceCreation(busKey, _queue, queueInfo.Name);
        }

        public async Task CreateDistributorAsync(string busKey, MessageDistributor distributor, CancellationToken cancellationToken = default)
        {
            var exchangeInfo = DistributorToExchangeInfo(distributor);
            await _managementClient.CreateExchangeAsync(distributor.Vhost, exchangeInfo, cancellationToken);
            LogResourceCreation(busKey, _exchange, exchangeInfo.Name);
        }

        public async Task CreateDistributorsWithDeliveriesAsync(string busKey, List<MessageDistributor> distributors, CancellationToken cancellationToken = default)
        {
            foreach (var exchange in distributors)
            {
                await CreateDistributorAsync(busKey, exchange, cancellationToken);
                foreach (var queue in exchange.Deliverymen)
                {
                    await CreateDeliverymanAsync(busKey, queue, exchange.Vhost, cancellationToken);
                    await BindDistributorAndDeliverymenAsync(busKey, exchange.Vhost, exchange.Name, queue.Name, queue.Name, cancellationToken);
                }
            }
        }

        private void LogResourceCreation(string busKey, string resourceType, string resourceName)
        {
            _logger.LogInformation($"Created {busKey} RabbitMQ {resourceType}: {resourceName}");
        }

        private ExchangeInfo DistributorToExchangeInfo(MessageDistributor distributor)
        {
            return new ExchangeInfo(distributor.Name, distributor.Type, distributor.AutoDelete, distributor.Durable, distributor.Internal);
        }

        private QueueInfo DeliverymanToQueueInfo(MessageDeliveryman deliveryman)
        {
            return new QueueInfo(deliveryman.Name, deliveryman.AutoDelete, deliveryman.Durable);
        }
    }
}
