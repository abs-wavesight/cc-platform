using Abs.CommonCore.Platform;

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
        public const string DrexVesselAdapter = "vessel";
        public const string DrexCentralAdapter = "central";
    }

    public static class ComposeEnvKeys
    {
        public const string WindowsVersion = "WINDOWS_VERSION";
        public const string PathToCommonCorePlatformRepository = "PATH_TO_CC_PLATFORM_REPO";
        public const string PathToCommonCoreDrexRepository = "PATH_TO_CC_DREX_REPO";
        public const string PathToCommonCoreDiscoRepository = "PATH_TO_CC_DISCO_REPO";
        public const string PathToCommonCoreSiemensAdapterRepository = "PATH_TO_CC_SIEMENS_ADAPTER_REPO";
        public const string PathToCommonCoreKdiAdapterRepository = "PATH_TO_CC_KDI_ADAPTER_REPO";
        public const string PathToCommonCoreVoyageManagerAdapterRepository = "PATH_TO_CC_VOYAGE_MANAGER_ADAPTER_REPO";
        public const string PathToCommonCoreSchedulerRepository = "PATH_TO_CC_SCHEDULER_REPO";
        public const string PathToCommonCoreDrexNotificationAdapterRepository = "PATH_TO_CC_DREX_NOTIFICATION_ADAPTER_REPO";
        public const string PathToCommonCoreSystemMonitorAdapterRepository = "PATH_TO_CC_SYSTEM_MONITOR_ADAPTER_REPO";
        public const string DrexSiteConfigFileNameOverride = "DREX_SITE_CONFIG_FILE_NAME";
        public const string PathToCertificates = "PATH_TO_CERTS";
        public static readonly string PathToSshKeys = PlatformConstants.SSH_Keys_Path;
        public static readonly string SftpRootPath = PlatformConstants.SFTP_Path;
        public static readonly string FdzRootPath = PlatformConstants.FDZ_Path;
        public static readonly string AbsCcClientsLogsPath = "ABS_CC_LOGS_PATH";
    }

    public static class CertificateSubDirectories
    {
        public const string LocalKeys = "local-keys";
        public const string LocalCerts = "local-certs";
        public const string RemoteKeys = "remote-keys";
        public const string RemoteCerts = "remote-certs";
    }
}
