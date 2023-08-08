using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Abs.CommonCore.Installer
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private const string ComponentNamePlaceholder = "$component";

        public static async Task<int> Main(string[] args)
        {
            var downloadCommand = SetupDownloadCommand(args);
            var installCommand = SetupInstallCommand(args);
            var chunkCommand = SetupChunkCommand(args);
            var unchunkCommand = SetupUnchunkCommand(args);
            var compressCommand = SetupCompressCommand(args);
            var uncompressCommand = SetupUncompressCommand(args);
            var releaseBodyCommand = SetupReleaseBodyCommand(args);
            var configureRabbitCommand = SetupConfigureRabbitCommand(args);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);
            root.Add(installCommand);
            root.Add(chunkCommand);
            root.Add(unchunkCommand);
            root.Add(compressCommand);
            root.Add(uncompressCommand);
            root.Add(releaseBodyCommand);
            root.Add(configureRabbitCommand);

            var result = await root.InvokeAsync(args);
            await Task.Delay(1000);
            return result;
        }

        private static Command SetupDownloadCommand(string[] args)
        {
            var command = new Command("download", "Download components for installation");
            command.TreatUnmatchedTokensAsErrors = true;

            var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration");
            registryParam.IsRequired = true;
            registryParam.AddAlias("-r");
            command.Add(registryParam);

            var downloadConfigParam = new Option<FileInfo>("--config", "Location of download configuration");
            downloadConfigParam.IsRequired = false;
            downloadConfigParam.AddAlias("-dc");
            command.Add(downloadConfigParam);

            var componentParam = new Option<string[]>("--component", "Specific component to process");
            componentParam.IsRequired = false;
            componentParam.AddAlias("-c");
            componentParam.AllowMultipleArgumentsPerToken = true;
            command.Add(componentParam);

            var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter");
            parameterParam.IsRequired = false;
            parameterParam.AddAlias("-p");
            parameterParam.AllowMultipleArgumentsPerToken = true;
            command.Add(parameterParam);

            var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
            verifyOnlyParam.SetDefaultValue(false);
            verifyOnlyParam.IsRequired = false;
            verifyOnlyParam.AddAlias("-v");
            command.Add(verifyOnlyParam);

            command.SetHandler(async (registryConfig, downloaderConfig, components, parameters, verifyOnly) =>
            {
                await ExecuteDownloadCommandAsync(registryConfig, downloaderConfig, components, parameters, verifyOnly, args);
            }, registryParam, downloadConfigParam, componentParam, parameterParam, verifyOnlyParam);

            return command;
        }

        private static Command SetupInstallCommand(string[] args)
        {
            var command = new Command("install", "Install components");
            command.TreatUnmatchedTokensAsErrors = true;

            var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration");
            registryParam.IsRequired = true;
            registryParam.AddAlias("-r");
            command.Add(registryParam);

            var installConfigParam = new Option<FileInfo>("--config", "Location of install configuration");
            installConfigParam.IsRequired = false;
            installConfigParam.AddAlias("-ic");
            command.Add(installConfigParam);

            var componentParam = new Option<string[]>("--component", "Specific component to process");
            componentParam.IsRequired = false;
            componentParam.AddAlias("-c");
            componentParam.AllowMultipleArgumentsPerToken = true;
            command.Add(componentParam);

            var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter");
            parameterParam.IsRequired = false;
            parameterParam.AddAlias("-p");
            parameterParam.AllowMultipleArgumentsPerToken = true;
            command.Add(parameterParam);

            var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
            verifyOnlyParam.SetDefaultValue(false);
            verifyOnlyParam.IsRequired = false;
            verifyOnlyParam.AddAlias("-v");
            command.Add(verifyOnlyParam);

            command.SetHandler(async (registryConfig, installerConfig, components, parameters, verifyOnly) =>
            {
                await ExecuteInstallCommandAsync(registryConfig, installerConfig, components, parameters, verifyOnly, args);
            }, registryParam, installConfigParam, componentParam, parameterParam, verifyOnlyParam);

            return command;
        }

        private static Command SetupChunkCommand(string[] args)
        {
            var command = new Command("chunk", "Break a file into multiple smaller pieces");
            command.TreatUnmatchedTokensAsErrors = true;

            var sourceParam = new Option<FileInfo>("--source", "File to separate into chunks");
            sourceParam.IsRequired = true;
            sourceParam.AddAlias("-s");
            command.Add(sourceParam);

            var destParam = new Option<DirectoryInfo>("--dest", "Directory to store pieces in");
            destParam.IsRequired = true;
            destParam.AddAlias("-d");
            command.Add(destParam);

            var sizeParam = new Option<int>("--size", "Size in bytes to limit chunks to");
            sizeParam.IsRequired = true;
            sizeParam.AddAlias("-sz");
            command.Add(sizeParam);

            var removeSourceParam = new Option<bool>("--remove-source", "Remove source file after processing");
            removeSourceParam.SetDefaultValue(false);
            removeSourceParam.IsRequired = false;
            removeSourceParam.AddAlias("-rs");
            command.Add(removeSourceParam);

            var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.");
            configParam.IsRequired = false;
            configParam.AddAlias("-c");
            command.Add(configParam);

            command.SetHandler(async (source, dest, size, removeSource, config) =>
            {
                await ExecuteChunkCommandAsync(source, dest, size, removeSource, config, args);
            }, sourceParam, destParam, sizeParam, removeSourceParam, configParam);

            return command;
        }

        private static Command SetupUnchunkCommand(string[] args)
        {
            var command = new Command("unchunk", "Merges file chunks into a single file");
            command.TreatUnmatchedTokensAsErrors = true;

            var sourceParam = new Option<DirectoryInfo>("--source", "Directory where file chunks are");
            sourceParam.IsRequired = true;
            sourceParam.AddAlias("-s");
            command.Add(sourceParam);

            var destParam = new Option<FileInfo>("--dest", "File to write combined pieces to");
            destParam.IsRequired = true;
            destParam.AddAlias("-d");
            command.Add(destParam);

            var removeSourceParam = new Option<bool>("--remove-source", "Remove source directory after processing");
            removeSourceParam.SetDefaultValue(false);
            removeSourceParam.IsRequired = false;
            removeSourceParam.AddAlias("-rs");
            command.Add(removeSourceParam);

            var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.");
            configParam.IsRequired = false;
            configParam.AddAlias("-c");
            command.Add(configParam);

            command.SetHandler(async (source, dest, removeSource, config) =>
            {
                await ExecuteUnchunkCommandAsync(source, dest, removeSource, config, args);
            }, sourceParam, destParam, removeSourceParam, configParam);

            return command;
        }

        private static Command SetupCompressCommand(string[] args)
        {
            var command = new Command("compress", "Compresses a directory into a single file");
            command.TreatUnmatchedTokensAsErrors = true;

            var sourceParam = new Option<DirectoryInfo>("--source", "Directory to compress");
            sourceParam.IsRequired = true;
            sourceParam.AddAlias("-s");
            command.Add(sourceParam);

            var destParam = new Option<FileInfo>("--dest", "File to write compressed contents to");
            destParam.IsRequired = true;
            destParam.AddAlias("-d");
            command.Add(destParam);

            var removeSourceParam = new Option<bool>("--remove-source", "Remove source directory after processing");
            removeSourceParam.SetDefaultValue(false);
            removeSourceParam.IsRequired = false;
            removeSourceParam.AddAlias("-rs");
            command.Add(removeSourceParam);

            var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.");
            configParam.IsRequired = false;
            configParam.AddAlias("-c");
            command.Add(configParam);

            command.SetHandler(async (source, dest, removeSource, config) =>
            {
                await ExecuteCompressCommandAsync(source, dest, removeSource, config, args);
            }, sourceParam, destParam, removeSourceParam, configParam);

            return command;
        }

        private static Command SetupUncompressCommand(string[] args)
        {
            var command = new Command("uncompress", "Uncompresses a file back into directory structure");
            command.TreatUnmatchedTokensAsErrors = true;

            var sourceParam = new Option<FileInfo>("--source", "File to uncompress");
            sourceParam.IsRequired = true;
            sourceParam.AddAlias("-s");
            command.Add(sourceParam);

            var destParam = new Option<DirectoryInfo>("--dest", "Directory to write contents to");
            destParam.IsRequired = true;
            destParam.AddAlias("-d");
            command.Add(destParam);

            var removeSourceParam = new Option<bool>("--remove-source", "Remove source file after processing");
            removeSourceParam.SetDefaultValue(false);
            removeSourceParam.IsRequired = false;
            removeSourceParam.AddAlias("-rs");
            command.Add(removeSourceParam);

            var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.");
            configParam.IsRequired = false;
            configParam.AddAlias("-c");
            command.Add(configParam);

            command.SetHandler(async (source, dest, removeSource, config) =>
            {
                await ExecuteUncompressCommandAsync(source, dest, removeSource, config, args);
            }, sourceParam, destParam, removeSourceParam, configParam);

            return command;
        }

        private static Command SetupReleaseBodyCommand(string[] args)
        {
            var command = new Command("release-body", "Builds a release body based on configuration");
            command.TreatUnmatchedTokensAsErrors = true;

            var configParam = new Option<FileInfo>("--config", $"Location of download configuration.");
            configParam.IsRequired = false;
            configParam.AddAlias("-c");
            command.Add(configParam);

            var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter");
            parameterParam.IsRequired = false;
            parameterParam.AddAlias("-p");
            parameterParam.AllowMultipleArgumentsPerToken = true;
            command.Add(parameterParam);

            var outputParam = new Option<FileInfo>("--output", "Location to save release body to");
            outputParam.IsRequired = true;
            outputParam.AddAlias("-o");
            command.Add(outputParam);

            command.SetHandler(async (config, parameters, output) =>
            {
                await ExecuteReleaseBodyBuilderCommandAsync(config, parameters, output, args);
            }, configParam, parameterParam, outputParam);

            return command;
        }

        private static Command SetupConfigureRabbitCommand(string[] args)
        {
            var command = new Command("configure-rabbit", "Configures rabbit mq with credentials for configuration file");
            command.TreatUnmatchedTokensAsErrors = true;

            var rabbitParam = new Option<Uri>("--rabbit", $"Http uri to connect to rabbit mq");
            rabbitParam.IsRequired = true;
            rabbitParam.AddAlias("-r");
            command.Add(rabbitParam);

            var rabbitUsernameParam = new Option<string>("--rabbit-user", "Username to connect to rabbit");
            rabbitUsernameParam.IsRequired = true;
            rabbitUsernameParam.AddAlias("-ru");
            command.Add(rabbitUsernameParam);

            var rabbitPasswordParam = new Option<string>("--rabbit-password", "Password to connect to rabbit");
            rabbitPasswordParam.IsRequired = true;
            rabbitPasswordParam.AddAlias("-rp");
            command.Add(rabbitPasswordParam);

            var usernameParam = new Option<string>("--user", "Username to use for account");
            usernameParam.IsRequired = true;
            usernameParam.AddAlias("-u");
            command.Add(usernameParam);

            var passwordParam = new Option<string>("--password", "Password to use for account");
            passwordParam.IsRequired = false;
            passwordParam.AddAlias("-p");
            command.Add(passwordParam);

            var updatePermissionsParam = new Option<bool>("--update-permissions", "Update the users permissions");
            updatePermissionsParam.IsRequired = false;
            updatePermissionsParam.AddAlias("-up");
            command.Add(updatePermissionsParam);

            var drexSiteConfigParam = new Option<FileInfo>("--drex-site-config", "Path to drex site config to update");
            drexSiteConfigParam.IsRequired = false;
            drexSiteConfigParam.AddAlias("-dsc");
            command.Add(drexSiteConfigParam);

            var superUserParam = new Option<bool>("--super-user", "Indicates the account is for a super user");
            superUserParam.IsRequired = false;
            superUserParam.SetDefaultValue(false);
            superUserParam.AddAlias("-su");
            command.Add(superUserParam);

            var credentialsFileParam = new Option<FileInfo>("--credentials-file", "Updates the file with the generated credentials");
            credentialsFileParam.IsRequired = false;
            credentialsFileParam.AddAlias("-cf");
            command.Add(credentialsFileParam);

            command.SetHandler(async (context) =>
            {
                var arguments = new RabbitConfigureCommandArguments
                {
                    Rabbit = context.ParseResult.GetValueForOption(rabbitParam),
                    RabbitUsername = context.ParseResult.GetValueForOption(rabbitUsernameParam),
                    RabbitPassword = context.ParseResult.GetValueForOption(rabbitPasswordParam),
                    Username = context.ParseResult.GetValueForOption(usernameParam),
                    Password = context.ParseResult.GetValueForOption(passwordParam),
                    UpdatePermissions = context.ParseResult.GetValueForOption(updatePermissionsParam),
                    DrexSiteConfig = context.ParseResult.GetValueForOption(drexSiteConfigParam),
                    SuperUser = context.ParseResult.GetValueForOption(superUserParam),
                    CredentialsFile = context.ParseResult.GetValueForOption(credentialsFileParam),
                };

                await ExecuteConfigureRabbitCommandAsync(arguments, args);
            });

            return command;
        }

        private static async Task ExecuteDownloadCommandAsync(FileInfo registryConfig, FileInfo downloaderConfig, string[] components, string[] parameters, bool verifyOnly, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);
            var configParameters = BuildConfigParameters(parameters);

            var dataRequest = new DataRequestService(loggerFactory, verifyOnly);
            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, registryConfig, downloaderConfig, configParameters);
            await downloader.ExecuteAsync(components);
        }

        private static async Task ExecuteInstallCommandAsync(FileInfo registryConfig, FileInfo installerConfig, string[] components, string[] parameters, bool verifyOnly, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);
            var configParameters = BuildConfigParameters(parameters);

            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var installer = new ComponentInstaller(loggerFactory, commandExecution, registryConfig, installerConfig, configParameters);
            await installer.ExecuteAsync(components);
        }

        private static async Task ExecuteChunkCommandAsync(FileInfo source, DirectoryInfo destination, int size, bool removeSource, FileInfo? config, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataChunker(loggerFactory);
            await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) =>
                {
                    await chunker.ChunkFileAsync(s, d, size, removeSource);
                });
        }

        private static async Task ExecuteUnchunkCommandAsync(DirectoryInfo source, FileInfo destination, bool removeSource, FileInfo? config, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataChunker(loggerFactory);
            await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) =>
                {
                    await chunker.UnchunkFileAsync(s, d, removeSource);
                });
        }

        private static async Task ExecuteCompressCommandAsync(DirectoryInfo source, FileInfo destination, bool removeSource, FileInfo? config, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var compressor = new DataCompressor(loggerFactory);
            await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) =>
                {
                    await compressor.CompressDirectoryAsync(s, d, removeSource);
                });
        }

        private static async Task ExecuteUncompressCommandAsync(FileInfo source, DirectoryInfo destination, bool removeSource, FileInfo? config, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var compressor = new DataCompressor(loggerFactory);
            await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) =>
                {
                    await compressor.UncompressFileAsync(s, d, removeSource);
                });
        }

        private static async Task ExecuteReleaseBodyBuilderCommandAsync(FileInfo? config, string[]? parameters, FileInfo output, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);
            var configParameters = BuildConfigParameters(parameters);

            var release = new ReleaseBodyBuilder(loggerFactory);
            await release.BuildReleaseBodyAsync(config, configParameters, output);
        }

        private static async Task ExecuteConfigureRabbitCommandAsync(RabbitConfigureCommandArguments arguments, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);
            var commandExecution = new CommandExecutionService(loggerFactory);
            var configurer = new RabbitConfigurer(loggerFactory, commandExecution);

            if (arguments.UpdatePermissions)
            {
                await configurer.UpdateUserPermissionsAsync(arguments.Rabbit!, arguments.RabbitUsername!, arguments.RabbitPassword!, arguments.Username!, arguments.SuperUser);
                return;
            }

            var credentials = await configurer.ConfigureRabbitAsync(arguments.Rabbit!, arguments.RabbitUsername!, arguments.RabbitPassword!, arguments.Username!, arguments.Password, arguments.SuperUser);

            if (credentials == null)
            {
                Console.WriteLine("No credentials added");
                return;
            }

            if (arguments.DrexSiteConfig != null) await configurer.UpdateDrexSiteConfigAsync(arguments.DrexSiteConfig, credentials);
            if (arguments.CredentialsFile != null) await configurer.UpdateCredentialsFileAsync(credentials, arguments.CredentialsFile);
        }

        private static async Task ExecuteForComponentsAsync(FileInfo source, DirectoryInfo destination, FileInfo? config, Func<FileInfo, DirectoryInfo, Task> action)
        {
            if (config == null)
            {
                await action(source, destination);
                return;
            }

            var downloaderConfig = ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(config.FullName);
            var components = downloaderConfig.Components;

            await components
                .ForAllAsync(async c =>
                {
                    var s = source.FullName.Replace(ComponentNamePlaceholder, c, StringComparison.OrdinalIgnoreCase);
                    var d = destination.FullName.Replace(ComponentNamePlaceholder, c, StringComparison.OrdinalIgnoreCase);

                    await action(new FileInfo(s), new DirectoryInfo(d));
                });
        }

        private static async Task ExecuteForComponentsAsync(DirectoryInfo source, FileInfo destination, FileInfo? config, Func<DirectoryInfo, FileInfo, Task> action)
        {
            if (config == null)
            {
                await action(source, destination);
                return;
            }

            var downloaderConfig = ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(config.FullName);
            var components = downloaderConfig.Components;

            await components
                .ForAllAsync(async c =>
                {
                    var s = source.FullName.Replace(ComponentNamePlaceholder, c, StringComparison.OrdinalIgnoreCase);
                    var d = destination.FullName.Replace(ComponentNamePlaceholder, c, StringComparison.OrdinalIgnoreCase);

                    await action(new DirectoryInfo(s), new FileInfo(d));
                });
        }

        private static (ILogger, ILoggerFactory) Initialize(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            return ConfigureLogging(builder.Logging);
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

        private static Dictionary<string, string> BuildConfigParameters(string[]? parameters)
        {
            if (parameters == null)
            {
                return new Dictionary<string, string>();
            }

            return parameters
                .Select(x =>
                {
                    var parts = x.Split(new char[] { ':' }, 2);
                    return new KeyValuePair<string, string>(parts[0], parts[1]);
                })
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
