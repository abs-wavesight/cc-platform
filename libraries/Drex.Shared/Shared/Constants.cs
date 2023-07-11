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
            public const string Source = $"{Prefix}.source";
            public const string Destination = $"{Prefix}.destination";
            public const string Sink = $"{Prefix}.sink";
        }

        public static class MessageBus
        {
            public const string ResourcePrefix = "cc.drex";

            public const string SiteClientDirectExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src.{DirectExchangeSuffix}";
            public const string SiteClientTopicExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src.{TopicExchangeSuffix}";
            public const string SiteClientDlqExchangeTemplate = $"{ResourcePrefix}.site.{{client}}-src-dlq.{TopicExchangeSuffix}";
            public const string SiteClientDlqTemplate = $"{ResourcePrefix}.site.{{client}}-src-dlq.{QueueSuffix}";

            public const string CentralClientDirectExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src.{DirectExchangeSuffix}";
            public const string CentralClientTopicExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src.{TopicExchangeSuffix}";
            public const string CentralClientDlqExchangeTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-dlq.{TopicExchangeSuffix}";
            public const string CentralClientDlqTemplate = $"{ResourcePrefix}.central.{{client}}-{{site}}-src-dlq.{QueueSuffix}";

            public const string SiteDlqExchangeName = $"{ResourcePrefix}.site.internal-src-dlq.{DirectExchangeSuffix}";
            public const string SiteDlqName = $"{ResourcePrefix}.site.internal-src-dlq.{QueueSuffix}";
            public const string CentralDlqName = $"{ResourcePrefix}.central.internal-src-dlq.{QueueSuffix}";
            public const string CentralDlqExchangeName = $"{ResourcePrefix}.central.internal-src-dlq.{DirectExchangeSuffix}";

            public const string SiteLogDirectExchangeName = $"{ResourcePrefix}.site.int-log.{DirectExchangeSuffix}";
            public const string SiteLogTopicExchangeName = $"{ResourcePrefix}.site.int-log.{TopicExchangeSuffix}";
            public const string SiteLogQueueName = $"{ResourcePrefix}.site.int-log.{QueueSuffix}";

            public const string CentralLogDirectExchangeTemplate = $"{ResourcePrefix}.central.{{app}}-snk-log.{DirectExchangeSuffix}";
            public const string CentralLogTopicExchangeTemplate = $"{ResourcePrefix}.central.{{app}}-snk-log.{TopicExchangeSuffix}";
            public const string CentralLogQueueTemplate = $"{ResourcePrefix}.central.{{app}}-snk-log.{QueueSuffix}";

            public const string QueueSuffix = "q";
            public const string DirectExchangeSuffix = "ed";
            public const string TopicExchangeSuffix = "et";
        }
    }
}
