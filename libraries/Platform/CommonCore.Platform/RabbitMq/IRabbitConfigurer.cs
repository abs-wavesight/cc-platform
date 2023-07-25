namespace Abs.CommonCore.Platform.RabbitMq
{
    public interface IRabbitConfigurer
    {
        Task CreateExchangeAsync(string busKey, string vhostName, string exName, string exType, bool autoDelete = false, bool isDurable = true, bool isInternal = false, CancellationToken cancellationToken = default);
        Task CreateExchangesWithQueuesAsync(string busKey, List<RabbitConfigurerExchange> exchanges, CancellationToken cancellationToken = default);
        Task CreateQueueAsync(string busKey, string vhostName, string queueName, bool autoDelete = false, bool durable = true, CancellationToken cancellationToken = default);
        Task CreateQueueBindingAsync(string busKey, string vhostName, string exchangeName, string queueName, string bindingName, CancellationToken cancellationToken = default);
    }
}
