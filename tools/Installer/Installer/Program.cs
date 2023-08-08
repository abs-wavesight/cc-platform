using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions;
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
            var uninstallCommand = SetupUninstallCommand(args);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);
            root.Add(installCommand);
            root.Add(chunkCommand);
            root.Add(unchunkCommand);
            root.Add(compressCommand);
            root.Add(uncompressCommand);
            root.Add(releaseBodyCommand);
            root.Add(uninstallCommand);

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

        private static Command SetupUninstallCommand(string[] args)
        {
            var command = new Command("uninstall", "Uninstalls all installed components");
            command.TreatUnmatchedTokensAsErrors = true;

            var dockerParam = new Option<DirectoryInfo>("--docker", "Path to docker location");
            dockerParam.IsRequired = false;
            dockerParam.AddAlias("-d");
            command.Add(dockerParam);

            var pathParam = new Option<DirectoryInfo>("--path", "Path to the installation location");
            pathParam.IsRequired = false;
            pathParam.AddAlias("-p");
            command.Add(pathParam);

            var removeSystemParam = new Option<bool>("--remove-system", "Indicates to remove system components");
            removeSystemParam.IsRequired = false;
            removeSystemParam.SetDefaultValue(false);
            command.Add(removeSystemParam);

            var removeConfigParam = new Option<bool>("--remove-config", "Indicates to remove configuration files");
            removeConfigParam.IsRequired = false; ;
            removeConfigParam.SetDefaultValue(false);
            command.Add(removeConfigParam);

            var removeDockerParam = new Option<bool>("--remove-docker", "Indicates to remove docker");
            removeDockerParam.IsRequired = false;
            removeDockerParam.SetDefaultValue(false);
            command.Add(removeDockerParam);

            command.SetHandler(async (docker, path, removeSystem, removeConfig, removeDocker) =>
            {
                await ExecuteUninstallCommandAsync(docker, path, removeSystem, removeConfig, removeDocker, args);
            }, dockerParam, pathParam, removeSystemParam, removeConfigParam, removeDockerParam);

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

        private static async Task ExecuteUninstallCommandAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath, bool? removeSystem, bool? removeConfig, bool? removeDocker, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);
            var commandExecution = new CommandExecutionService(loggerFactory);

            var uninstaller = new Uninstaller(loggerFactory, commandExecution);
            await uninstaller.UninstallSystemAsync(dockerLocation, installPath, removeSystem, removeConfig, removeDocker);
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
