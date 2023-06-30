using Abs.CommonCore.Drex.Shared.Extensions;
using System.ComponentModel;

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

        [Description(Constants.MessageHeaders.Source)]
        public string? Source { get; set; }

        [Description(Constants.MessageHeaders.Destination)]
        public string? Destination { get; set; }

        [Description(Constants.MessageHeaders.Sink)]
        public string? Sink { get; set; }

        private MessageMetadata(Dictionary<string, string> headers)
        {
            headers.TryGetValue(GetType().GetDescription(nameof(Client))!, out var client);
            Client = client;

            headers.TryGetValue(GetType().GetDescription(nameof(Site))!, out var site);
            Site = site;

            headers.TryGetValue(GetType().GetDescription(nameof(Origin))!, out var origin);
            Origin = origin;

            headers.TryGetValue(GetType().GetDescription(nameof(Source))!, out var source);
            Source = source;

            headers.TryGetValue(GetType().GetDescription(nameof(Destination))!, out var destination);
            Destination = destination;

            headers.TryGetValue(GetType().GetDescription(nameof(Sink))!, out var sink);
            Sink = sink;
        }

        public static MessageMetadata FromHeaders(Dictionary<string, string> headers)
        {
            return new MessageMetadata(headers);
        }
    }
}
