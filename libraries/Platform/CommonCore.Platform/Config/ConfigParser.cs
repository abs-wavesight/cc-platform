using System.Text;
using System.Text.Json.Nodes;
using Abs.CommonCore.Platform.Exceptions;
using Abs.CommonCore.Platform.Extensions;
using Contracts.Helpers.Extensions;
using Json.Schema;
using Microsoft.Extensions.Configuration;

namespace Abs.CommonCore.Platform.Config
{
    public static class ConfigParser
    {
        public static TConfig LoadConfig<TConfig>(string filePath)
            where TConfig : class, new()
        {
            GuardAgainstMissingFile<TConfig>(filePath);
            GuardAgainstInvalidJsonSchema<TConfig>(filePath);

            var siteConfigRaw = new ConfigurationBuilder()
                .AddJsonFile(filePath, optional: false)
                .Build();
            return siteConfigRaw.Bind<TConfig>();
        }

        public static TConfig LoadConfig<TConfig>(string filePath, Func<TConfig, string, string> preprocess)
            where TConfig : class, new()
        {
            GuardAgainstMissingFile<TConfig>(filePath);
            GuardAgainstInvalidJsonSchema<TConfig>(filePath);

            var siteConfigRaw = new ConfigurationBuilder()
                .AddJsonFile(filePath, optional: false)
                .Build();
            var config = siteConfigRaw.Bind<TConfig>();

            // Transform the config as needed
            var configText = File.ReadAllText(filePath);
            var transformed = preprocess(config, configText);

            // Parse transformed JSON
            var clientAppConfigRaw = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(transformed)))
                .Build();
            return clientAppConfigRaw.Bind<TConfig>();
        }

        private static void GuardAgainstMissingFile<T>(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                throw new ConfigException(typeof(T), configFilePath, "Config file not found");
            }
        }

        private static void GuardAgainstInvalidJsonSchema<T>(string configFilePath)
        {
            var configRawFileContents = File.ReadAllText(configFilePath);
            var configJsonObject = JsonNode.Parse(configRawFileContents);

            var schemaObject = (T)Activator.CreateInstance(typeof(T))!;
            var schemaText = schemaObject.ToJsonSchema();
            var jsonSchema = JsonSchema.FromText(schemaText);
            var schemaEvaluationResult = jsonSchema.Evaluate(
                configJsonObject,
                new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (schemaEvaluationResult.IsValid) return;

            var errors = schemaEvaluationResult.Details
                .Where(_ => _.HasErrors)
                .SelectMany(detail => detail.Errors!.Select(error => $"{detail.InstanceLocation} > {detail.EvaluationPath}: {error.Value}"))
                .ToList();

            throw new ConfigException(typeof(T), configFilePath, errors.StringJoin(";"));
        }
    }
}
