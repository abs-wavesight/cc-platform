using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Drex.Shared.MessageBus.Extensions;
using Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;
using Microsoft.Extensions.Logging;
using Rebus.Compression;
using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.Retry.Simple;
using Rebus.Serialization;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus
{
    public static class RebusConfigExtensions
    {
        public static RebusConfigurer ConfigureRebusConsumer(
            this RebusConfigurer rebusConfigurer,
            BusConnection sourceMessageBusConnectionInfo,
            string busName,
            string inputQueueName,
            string? deadLetterQueueName,
            string directExchangeName,
            string topicExchangeName,
            ILoggerFactory loggerFactory,
            Action<StandardConfigurer<ISerializer>>? serializerConfig = null,
            bool enableSsl = false)
        {
            return rebusConfigurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t =>
                {
                    var rabbitMqOptions = t
                        .UseRabbitMq(
                            sourceMessageBusConnectionInfo.ToConnectionString(),
                            inputQueueName)
                        .SetPublisherConfirms(true)
                        .ExchangeNames(
                            directExchangeName: directExchangeName,
                            topicExchangeName: topicExchangeName)
                        .Declarations(declareExchanges: false, declareInputQueue: false);
                    if (enableSsl)
                    {
                        rabbitMqOptions.Ssl(new SslSettings(true, sourceMessageBusConnectionInfo.Host));
                    }
                })
                .Options(o =>
                {
                    o.SetBusName(busName);
                    if (string.IsNullOrWhiteSpace(deadLetterQueueName) == false) o.SimpleRetryStrategy(errorQueueAddress: deadLetterQueueName);
                    // o.LogPipeline(verbose: true);
                })
                .SetSerialization(serializerConfig);

            if (enableSsl)
            {
            }

            return configurer;
        }

        public static RebusConfigurer ConfigureRebusPublisher(
            this RebusConfigurer rebusConfigurer,
            BusConnection busConnection,
            string busName,
            string directExchangeName,
            string topicExchangeName,
            ILoggerFactory loggerFactory,
            Action<StandardConfigurer<ISerializer>>? serializerConfig = null,
            bool enableSsl = false)
        {
            return rebusConfigurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t =>
                {
                    var rabbitMqOptions = t
                        .UseRabbitMqAsOneWayClient(busConnection.ToConnectionString())
                        .SetPublisherConfirms(true)
                        .ExchangeNames(
                            directExchangeName: directExchangeName,
                            topicExchangeName: topicExchangeName)
                        .Declarations(declareExchanges: false, declareInputQueue: false);
                    if (enableSsl)
                    {
                        rabbitMqOptions.Ssl(new SslSettings(true, busConnection.Host));
                    }
                })
                .Options(o =>
                {
                    o.SetBusName(busName);
                    o.EnableCompression(1);
                    // o.LogPipeline(verbose: true);
                })
                .SetSerialization(serializerConfig);
        }

        private static RebusConfigurer SetSerialization(
            this RebusConfigurer rebusConfigurer,
            Action<StandardConfigurer<ISerializer>>? serializerConfig = null)
        {
            if (serializerConfig == null)
            {
                return rebusConfigurer
                    .Serialization(s => s.Register(_ => new StringMessageSerializer()));
            }

            return rebusConfigurer
                .Serialization(s => serializerConfig(s));
        }
    }
}
