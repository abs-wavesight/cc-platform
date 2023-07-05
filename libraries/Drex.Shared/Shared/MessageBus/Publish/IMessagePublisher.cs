namespace Abs.CommonCore.Drex.Shared.MessageBus.Publish
{
    public interface IMessagePublisher
    {
        public string BusKey { get; }

        Task PublishAsync(object message, string destinationQueueName, Dictionary<string, string> headers);
    }
}
