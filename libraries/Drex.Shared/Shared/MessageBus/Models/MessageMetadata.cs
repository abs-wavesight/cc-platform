using System.ComponentModel;
using Abs.CommonCore.Platform.Extensions;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Models
{
    public class MessageMetadata
    {
        [Description(Constants.MessageHeaders.Client)]
        public string? Client { get; set; }

        [Description(Constants.MessageHeaders.Site)]
        public string? Site { get; set; }

        [Description(Constants.MessageHeaders.Origin)]
        public string? Origin { get; set; }

        [Description(Constants.MessageHeaders.Destination)]
        public string? Destination { get; set; }

        [Description(Constants.MessageHeaders.MessageType)]
        public string? MessageType { get; set; }

        [Description(Constants.MessageHeaders.DestinationClient)]
        public string? DestinationClient { get; set; }

        private MessageMetadata(IReadOnlyDictionary<string, string> headers)
        {
            headers.TryGetValue(GetType().GetDescription(nameof(Client))!, out var client);
            Client = client;

            headers.TryGetValue(GetType().GetDescription(nameof(Site))!, out var site);
            Site = site;

            headers.TryGetValue(GetType().GetDescription(nameof(Origin))!, out var origin);
            Origin = origin;

            headers.TryGetValue(GetType().GetDescription(nameof(Destination))!, out var destination);
            Destination = destination;

            headers.TryGetValue(GetType().GetDescription(nameof(MessageType))!, out var messageType);
            MessageType = messageType;

            headers.TryGetValue(GetType().GetDescription(nameof(DestinationClient))!, out var destinationClient);
            DestinationClient = destinationClient;
        }

        public static MessageMetadata FromHeaders(IReadOnlyDictionary<string, string> headers)
        {
            return new MessageMetadata(headers);
        }
    }
}
