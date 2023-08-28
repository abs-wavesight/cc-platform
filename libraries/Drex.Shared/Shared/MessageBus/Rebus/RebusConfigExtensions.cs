using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Drex.Shared.MessageBus.Extensions;
using Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;
using Microsoft.Extensions.Logging;
using Rebus.Compression;
using Rebus.Config;
using Rebus.Messages;
using Rebus.RabbitMq;
using Rebus.Retry.Simple;
using Rebus.Routing.TransportMessages;
using Rebus.Serialization;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus
{
    public class CommonCoreServiceBusOptions
    {
        public required ILoggerFactory LoggerFactory { get; init; }
        public required BusConnection BusConnectionInfo { get; init; }
        public required string InputQueueName { get; set; }
        public required string BusName { get; set; }
        public string? DeadLetterQueueName { get; set; }
        public Action<StandardConfigurer<ISerializer>>? SerializerConfig { get; set; }

        public CommonCoreRabbitMqOptions? RabbitMqOptions { get; init; }
        public CommonCoreAzureServiceBusOptions? AzureServiceBusOptions { get; init; }
    }

    public class CommonCoreRabbitMqOptions
    {
        public required string DirectExchangeName { get; set; }
        public required string TopicExchangeName { get; set; }
        public required bool EnableSsl { get; set; }
    }

    public class CommonCoreAzureServiceBusOptions
    {

    }

    public static class RebusConfigExtensions
    {
        private static void ValidateOptionsAndThrowIfInvalid(CommonCoreServiceBusOptions options)
        {
            if (options.RabbitMqOptions != null && options.AzureServiceBusOptions != null)
            {
                throw new ArgumentException("Only one of RabbitMqOptions or AzureServiceBusOptions can be set.", nameof(options));
            }

            if (options.RabbitMqOptions == null && options.AzureServiceBusOptions == null)
            {
                throw new ArgumentException("Either RabbitMqOptions or AzureServiceBusOptions must be set.", nameof(options));
            }
        }

        public static RebusConfigurer ConfigureRebusConsumer(this RebusConfigurer rebusConfigurer, CommonCoreServiceBusOptions options)
        {
            ValidateOptionsAndThrowIfInvalid(options);

            return rebusConfigurer
                .Logging(l => l.MicrosoftExtensionsLogging(options.LoggerFactory))
                .Transport(t =>
                {
                    if (options.RabbitMqOptions != null)
                    {
                        var rabbitMqOptions = t
                            .UseRabbitMq(
                                options.BusConnectionInfo.ToConnectionString(),
                                options.InputQueueName)
                            .SetPublisherConfirms(true)
                            .ExchangeNames(
                                directExchangeName: options.RabbitMqOptions.DirectExchangeName,
                                topicExchangeName: options.RabbitMqOptions.TopicExchangeName)
                            .Declarations(declareExchanges: false, declareInputQueue: false);

                        if (options.RabbitMqOptions.EnableSsl)
                        {
                            rabbitMqOptions.Ssl(new SslSettings(true, options.BusConnectionInfo.Host));
                        }
                    }
                    else if (options.AzureServiceBusOptions != null)
                    {
                        var azureServiceBusOptions = t
                            .UseAzureServiceBus(
                                options.BusConnectionInfo.ToConnectionString(),
                                options.InputQueueName);
                    }
                })
                .Options(o =>
                {
                    o.SetBusName(options.BusName);
                    if (string.IsNullOrWhiteSpace(options.DeadLetterQueueName) == false) o.SimpleRetryStrategy(errorQueueAddress: options.DeadLetterQueueName);
                    // o.LogPipeline(verbose: true);
                })
                .SetSerialization(options.SerializerConfig);
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

        public static RebusConfigurer ConfigureRebusQueueConsumer(
            this RebusConfigurer rebusConfigurer,
            BusConnection sourceMessageBusConnectionInfo,
            string busName,
            string inputQueueName,
            string? deadLetterQueueName,
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
                        .SetPublisherConfirms(true);
                    if (enableSsl)
                    {
                        rabbitMqOptions.Ssl(new SslSettings(true, sourceMessageBusConnectionInfo.Host));
                    }
                })
                .Options(o =>
                {
                    o.SetBusName(busName);
                    if (string.IsNullOrWhiteSpace(deadLetterQueueName) == false)
                        o.SimpleRetryStrategy(errorQueueAddress: deadLetterQueueName);
                })
                .SetSerialization(serializerConfig);
        }

        public static RebusConfigurer ConfigureRebusQueueConsumer(
            this RebusConfigurer rebusConfigurer,
            BusConnection sourceMessageBusConnectionInfo,
            string busName,
            string inputQueueName,
            Func<TransportMessage, Task<ForwardAction>> messageForwarder,
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
                        .SetPublisherConfirms(true);
                    if (enableSsl)
                    {
                        rabbitMqOptions.Ssl(new SslSettings(true, sourceMessageBusConnectionInfo.Host));
                    }
                })
                .Routing(r => r.AddTransportMessageForwarder(transportMessage => messageForwarder(transportMessage)))
                .Options(o => o.SetBusName(busName))
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
