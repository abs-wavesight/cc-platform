using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Installer.Actions.Downloader;
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

            var configParam = new Option<FileInfo>("--config", "Location of configuration");
            configParam.IsRequired = true;
            configParam.AddAlias("-c");
            downloadCommand.Add(configParam);

            var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
            verifyOnlyParam.SetDefaultValue(false);
            verifyOnlyParam.IsRequired = false;
            verifyOnlyParam.AddAlias("-v");
            downloadCommand.Add(verifyOnlyParam);

            downloadCommand.SetHandler(async (config, verifyOnly) =>
            {
                await ExecuteDownloadCommandAsync(config, verifyOnly, args);
            }, configParam, verifyOnlyParam);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);
            
            return await root.InvokeAsync(args);
        }

        private static async Task ExecuteDownloadCommandAsync(FileInfo config, bool verifyOnly, string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var (_, loggerFactory) = ConfigureLogging(builder.Logging);

            var dataRequest = new DataRequestService(loggerFactory, verifyOnly);
            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, config);
            await downloader.ExecuteAsync();
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