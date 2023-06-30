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

            public const string SiteSourceDirectExchangeName = $"{ResourcePrefix}.site.src.{DirectExchangeSuffix}";
            public const string SiteSourceTopicExchangeName = $"{ResourcePrefix}.site.src.{TopicExchangeSuffix}";
            public const string SiteSourceDlqQueueName = $"{ResourcePrefix}.site.src-dlq.{QueueSuffix}";
            public const string CentralSinkExchangeFormat = $"{ResourcePrefix}.{{0}}.{{1}}-snk";
            public const string QueueSuffix = "q";
            public const string DirectExchangeSuffix = "ed";
            public const string TopicExchangeSuffix = "et";
        }
    }
}
