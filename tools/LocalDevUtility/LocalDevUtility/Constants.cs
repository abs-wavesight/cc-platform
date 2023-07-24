namespace Abs.CommonCore.LocalDevUtility;

public static class Constants
{
    public const string ConfigFileName = "cc-local.config.json";
    public const string DockerComposeExecutionRootPath = "compose/local-dev";
    public const string EnvFileRelativePath = $"{DockerComposeExecutionRootPath}/.env";
    public const string ContainerRepository = "ghcr.io/abs-wavesight";
    public const string LogsContainingDirectoryName = "logs";
    public const string LogsSiteDirectoryName = "site";
    public const string LogsCentralDirectoryName = "central";
    public const string NugetConfigFileName = "nuget.config";
    public const string DefaultDrexSiteConfigFileName = "drex-demo-site-alpha.site-config.json";

    public static class Profiles
    {
        public const string RabbitMqLocal = "rabbitmq-local";
        public const string RabbitMqRemote = "rabbitmq-remote";
        public const string VectorSite = "vector-site";
        public const string VectorCentral = "vector-central";
    }

    public static class ComposeEnvKeys
    {
        public const string WindowsVersion = "WINDOWS_VERSION";
        public const string PathToCommonCorePlatformRepository = "PATH_TO_CC_PLATFORM_REPO";
        public const string PathToCommonCoreDrexRepository = "PATH_TO_CC_DREX_REPO";
        public const string DrexSiteConfigFileNameOverride = "DREX_SITE_CONFIG_FILE_NAME";
    }
}
