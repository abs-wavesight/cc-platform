using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Installer.Actions.Downloader;
using Abs.CommonCore.Installer.Actions.Installer;
using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Abs.CommonCore.Installer
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var downloadCommand = new Command("download", "Download components for installation");
            downloadCommand.TreatUnmatchedTokensAsErrors = true;

            var installCommand = new Command("install", "Install components");
            installCommand.TreatUnmatchedTokensAsErrors = true;

            var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration");
            registryParam.IsRequired = true;
            registryParam.AddAlias("-r");
            downloadCommand.Add(registryParam);
            installCommand.Add(registryParam);

            var downloadConfigParam = new Option<FileInfo>("--download", "Location of download configuration");
            downloadConfigParam.IsRequired = false;
            downloadConfigParam.AddAlias("-d");
            downloadCommand.Add(downloadConfigParam);

            var installConfigParam = new Option<FileInfo>("--install", "Location of install configuration");
            installConfigParam.IsRequired = false;
            installConfigParam.AddAlias("-i");
            installCommand.Add(installConfigParam);

            var componentParam = new Option<string[]>("--component", "Specific component to process");
            componentParam.IsRequired = false;
            componentParam.AddAlias("-c");
            componentParam.AllowMultipleArgumentsPerToken = true;
            downloadCommand.Add(componentParam);
            installCommand.Add(componentParam);

            var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
            verifyOnlyParam.SetDefaultValue(false);
            verifyOnlyParam.IsRequired = false;
            verifyOnlyParam.AddAlias("-v");
            downloadCommand.Add(verifyOnlyParam);
            installCommand.Add(verifyOnlyParam);

            downloadCommand.SetHandler(async (registryConfig, downloaderConfig, components, verifyOnly) =>
            {
                await ExecuteDownloadCommandAsync(registryConfig, downloaderConfig, components, verifyOnly, args);
            }, registryParam, downloadConfigParam, componentParam, verifyOnlyParam);

            installCommand.SetHandler(async (registryConfig, installerConfig, components, verifyOnly) =>
            {
                await ExecuteInstallCommandAsync(registryConfig, installerConfig, components, verifyOnly, args);
            }, registryParam, installConfigParam, componentParam, verifyOnlyParam);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);
            root.Add(installCommand);

            var result = await root.InvokeAsync(args);
            await Task.Delay(1000);
            return result;
        }

        private static async Task ExecuteDownloadCommandAsync(FileInfo registryConfig, FileInfo downloaderConfig, string[] components, bool verifyOnly, string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var (_, loggerFactory) = ConfigureLogging(builder.Logging);

            var dataRequest = new DataRequestService(loggerFactory, verifyOnly);
            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, registryConfig, downloaderConfig);
            await downloader.ExecuteAsync(components);
        }

        private static async Task ExecuteInstallCommandAsync(FileInfo registryConfig, FileInfo installerConfig, string[] components, bool verifyOnly, string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var (_, loggerFactory) = ConfigureLogging(builder.Logging);

            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var installer = new ComponentInstaller(loggerFactory, commandExecution, registryConfig, installerConfig);
            await installer.ExecuteAsync(components);
        }

        private static (ILogger, ILoggerFactory) ConfigureLogging(ILoggingBuilder builder)
        {
            builder.ClearProviders();

            // When debugging locally, simple console output is easier to read than JSON; but when deployed, we want structured JSON logs
#if DEBUG
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss:ffffff ";
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
#else
            builder.AddJsonConsole(options =>
            {
                options.TimestampFormat = "u";
                options.IncludeScopes = true;
            });
#endif

            var provider = builder.Services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<Program>>();
            builder.Services.AddSingleton<ILogger>(logger);

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            return (logger, loggerFactory);
        }
    }
}
