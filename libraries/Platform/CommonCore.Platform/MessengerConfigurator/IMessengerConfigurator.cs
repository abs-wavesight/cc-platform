namespace Abs.CommonCore.Platform.MessengerConfigurator
{
    public interface IMessengerConfigurator
    {
        Task CreateDistributorAsync(string busKey, MessageDistributor distributor, CancellationToken cancellationToken = default);
        Task CreateDistributorsWithDeliveriesAsync(string busKey, List<MessageDistributor> distributors, CancellationToken cancellationToken = default);
        Task CreateDeliverymanAsync(string busKey, MessageDeliveryman deliveryman, string vhost, CancellationToken cancellationToken = default);
        Task BindDistributorAndDeliverymenAsync(string busKey, string distributorName, string deliverymanName, string bindingName = "", string vhostName = "/", CancellationToken cancellationToken = default);
    }
}
