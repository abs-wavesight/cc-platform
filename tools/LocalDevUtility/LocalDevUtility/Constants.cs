using Abs.CommonCore.Platform;

namespace Abs.CommonCore.LocalDevUtility;

public static class Constants
{
    public static readonly string ConfigFileName = "cc-local.config.json";
    public static readonly string DockerComposeExecutionRootPath = "compose/local-dev";
    public static readonly string EnvFileRelativePath = $"{DockerComposeExecutionRootPath}/.env";
    public static readonly string ContainerRepository = "ghcr.io/abs-wavesight";
    public static readonly string LogsContainingDirectoryName = "logs";
    public static readonly string LogsSiteDirectoryName = "site";
    public static readonly string LogsCentralDirectoryName = "central";
    public static readonly string NugetConfigFileName = "nuget.config";
    public static readonly string DefaultDrexSiteConfigFileName = "drex-demo-site-alpha.site-config.json";

    public static class Profiles
    {
        public const string RabbitMqLocal = "rabbitmq-local";
        public const string RabbitMqRemote = "rabbitmq-remote";
        public const string VectorSite = "vector-site";
        public const string VectorCentral = "vector-central";
    }

    public static class ComposeEnvKeys
    {
        public static readonly string WindowsVersion = "WINDOWS_VERSION";
        public static readonly string PathToCommonCorePlatformRepository = "PATH_TO_CC_PLATFORM_REPO";
        public static readonly string PathToCommonCoreDrexRepository = "PATH_TO_CC_DREX_REPO";
        public static readonly string DrexSiteConfigFileNameOverride = "DREX_SITE_CONFIG_FILE_NAME";
        public static readonly string PathToCertificates = "PATH_TO_CERTS";
        public static readonly string PathToSshKeys = PlatformConstants.SSH_Keys_Path;
        public static readonly string SftpRootPath = PlatformConstants.SFTP_Path;
        public static readonly string FdzRootPath = PlatformConstants.FDZ_Path;
    }

    public static class CertificateSubDirectories
    {
        public static readonly string LocalKeys = "local-keys";
        public static readonly string LocalCerts = "local-certs";
        public static readonly string RemoteKeys = "remote-keys";
        public static readonly string RemoteCerts = "remote-certs";
    }
}
