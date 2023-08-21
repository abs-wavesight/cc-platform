namespace Abs.CommonCore.Drex.Shared
{
    public static class Constants
    {
        public static class MessageHeaders
        {
            public const string Prefix = "cc.drex";

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

                public const string SiteFileRequestDirectExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-request.{DirectExchangeSuffix}";
                public const string SiteFileRequestTopicExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-request.{TopicExchangeSuffix}";
                public const string SiteFileResponseDirectExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-response.{DirectExchangeSuffix}";
                public const string SiteFileResponseTopicExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-response.{TopicExchangeSuffix}";
                public const string SiteFileNotificationDirectExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-notification.{DirectExchangeSuffix}";
                public const string SiteFileNotificationTopicExchangeTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-notification.{TopicExchangeSuffix}";

                public const string SiteFileRequestQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-request.{QueueSuffix}";
                public const string SiteFileResponseQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-response.{QueueSuffix}";
                public const string SiteFileNotificationQueueTemplate = $"cc.{DrexFileReservedName}.site.{{client}}-notification.{QueueSuffix}";
            }

            public static class Message
            {
                public const string SiteClientSourceDirectExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src.{DirectExchangeSuffix}";
                public const string SiteClientSourceTopicExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src.{TopicExchangeSuffix}";
                public const string SiteClientSinkDirectExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-snk.{DirectExchangeSuffix}";
                public const string SiteClientSinkTopicExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-snk.{TopicExchangeSuffix}";
                public const string SiteClientDlqExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src-dlq.{TopicExchangeSuffix}";
                public const string SiteClientDlqTemplate = $"{ResourcePrefix}.site.{{client}}-src-dlq.{QueueSuffix}";

                public const string CentralClientDirectExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src.{DirectExchangeSuffix}";
                public const string CentralClientTopicExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src.{TopicExchangeSuffix}";
                public const string CentralClientDlqExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-dlq.{TopicExchangeSuffix}";
                public const string CentralClientDlqTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-dlq.{QueueSuffix}";

                public const string SiteDlqDirectExchangeName = $"{ResourcePrefix}.site.{InternalSourceDlqReservedName}.{DirectExchangeSuffix}";
                public const string SiteDlqTopicExchangeName = $"{ResourcePrefix}.site.{InternalSourceDlqReservedName}.{TopicExchangeSuffix}";
                public const string SiteDlqName = $"{ResourcePrefix}.site.{InternalSourceDlqReservedName}.{QueueSuffix}";
                public const string CentralDlqName = $"{ResourcePrefix}.central.{InternalSourceDlqReservedName}.{QueueSuffix}";
                public const string CentralDlqExchangeName = $"{ResourcePrefix}.central.{InternalSourceDlqReservedName}.{DirectExchangeSuffix}";

                public const string SiteLogDirectExchangeName = $"{ResourcePrefix}.site.{InfrastructureLogsReservedName}.{DirectExchangeSuffix}";
                public const string SiteLogTopicExchangeName = $"{ResourcePrefix}.site.{InfrastructureLogsReservedName}.{TopicExchangeSuffix}";
                public const string SiteLogQueueName = $"{ResourcePrefix}.site.{InfrastructureLogsReservedName}.{QueueSuffix}";

                public const string CentralLogDirectExchangeTemplate = $"{ResourcePrefix}.central.{SinkLogsReservedName}.{DirectExchangeSuffix}";
                public const string CentralLogTopicExchangeTemplate = $"{ResourcePrefix}.central.{SinkLogsReservedName}.{TopicExchangeSuffix}";
                public const string CentralLogQueueTemplate = $"{ResourcePrefix}.central.{SinkLogsReservedName}.{QueueSuffix}";

                public const string ConfigSiteSourceTemplate = $"{ResourcePrefix}.site.{{client}}-src-{{name}}.{QueueSuffix}";
                public const string ConfigSiteSinkTemplate = $"cc.{{client}}.site.snk-{{name}}.{QueueSuffix}";
                public const string ConfigRemoteSourceTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-{{name}}.{QueueSuffix}";
                public const string ConfigRemoteSinkTemplate = $"cc.{{client}}.central.{{site}}-snk-{{name}}.{QueueSuffix}";
            }
        }
    }
}
