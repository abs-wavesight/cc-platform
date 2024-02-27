namespace Abs.CommonCore.Drex.Shared;

public static class Constants
{
    public static class MessageHeaders
    {
        public const string Prefix = "cc";

        public const string Client = $"{Prefix}.client";
        public const string Site = $"{Prefix}.site";
        public const string Origin = $"{Prefix}.origin";
        public const string Destination = $"{Prefix}.destination";
        public const string MessageType = $"{Prefix}.message-type";
        public const string DestinationClient = $"{Prefix}.destination-client";

        public const string ContainerPrefix = "container";
        public const string ContainerName = $"{ContainerPrefix}.name";
        public const string ContainerType = $"{ContainerPrefix}.type";
        public const string ContainerImage = $"{ContainerPrefix}.image";
        public const string VesselImo = $"{Prefix}.vessel.imo";
    }

    public static class MessageBus
    {
        public const string InternalSourceDlqReservedName = "internal-src-dlq";
        public const string InfrastructureLogsReservedName = "int-log";
        public const string SinkLogsReservedName = "snk-log";

        public const string ResourcePrefix = "cc.drex";

        public const string QueueSuffix = "q";
        public const string DirectExchangeSuffix = "ed";
        public const string TopicExchangeSuffix = "et";

        public static class File
        {
            public const string DrexFileReservedName = "drex-file";

            public const string SiteFileRequestQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-request.{QueueSuffix}";
            public const string SiteFileResponseQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-response.{QueueSuffix}";
            public const string SiteFileNotificationQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-notification.{QueueSuffix}";
        }

        public static class Message
        {
            public const string DirectExchangeName = $"{ResourcePrefix}.{DirectExchangeSuffix}";
            public const string TopicExchangeName = $"{ResourcePrefix}.{TopicExchangeSuffix}";

            public const string SiteClientConfigErrorTemplate = $"{ResourcePrefix}.site.{{client}}-config-error.{QueueSuffix}";
            public const string SiteClientDlqTemplate = $"{ResourcePrefix}.site.{{client}}-src-dlq.{QueueSuffix}";

            public const string CentralClientDlqTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-dlq.{QueueSuffix}";

            public const string SiteDlqName = $"{ResourcePrefix}.site.{InternalSourceDlqReservedName}.{QueueSuffix}";
            public const string CentralDlqName = $"{ResourcePrefix}.central.{InternalSourceDlqReservedName}.{QueueSuffix}";

            public const string SiteLogQueueName = $"{ResourcePrefix}.site.{InfrastructureLogsReservedName}.{QueueSuffix}";

            public const string CentralLogQueueTemplate = $"{ResourcePrefix}.central.{SinkLogsReservedName}.{QueueSuffix}";

            public const string ConfigSiteSourceTemplate = $"{ResourcePrefix}.site.{{client}}-src-{{name}}.{QueueSuffix}";
            public const string ConfigSiteSinkTemplate = $"cc.{{client}}.site.snk-{{name}}.{QueueSuffix}";
            public const string ConfigRemoteSourceTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-{{name}}.{QueueSuffix}";
            public const string ConfigRemoteSinkTemplate = $"cc.{{client}}.central.{{site}}-snk-{{name}}.{QueueSuffix}";
        }
    }
}
