using Abs.CommonCore.Platform.MessengerConfigurator.Models;

namespace Abs.CommonCore.Platform.MessengerConfigurator;

public interface IMessengerConfigurator
{
    Task CreateExchangeAsync(string busKey, ExchangeInfo exchange, CancellationToken cancellationToken = default);
    Task CreateQueueAsync(string busKey, QueueInfo queue, CancellationToken cancellationToken = default);
    Task CreateExchangeWithQueuesAsync(string busKey, ExchangeInfo exchange, QueueInfo[] queues, CancellationToken cancellationToken = default);
    Task BindExchangeAndQueueAsync(string busKey, string exchange, string queue, string routing = "", string vhost = "/", CancellationToken cancellationToken = default);
    Task BindExchangeAndQueueAsync(string busKey, ExchangeInfo exchange, QueueInfo queue, string routing = "", CancellationToken cancellationToken = default);
}
