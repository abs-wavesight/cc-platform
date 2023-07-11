namespace CommonCore.Platform.Exceptions
{
    public class ConfigException : Exception
    {
        public ConfigException(Type schemaType, string configFilePath, string errorReport)
            : base($"Configuration was invalid (schema={schemaType.FullName}, config={configFilePath}). Errors: {errorReport}")
        {
        }
    }
}
