using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Installer.Actions.Chunker;
using Abs.CommonCore.Installer.Actions.Compression;
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
            var downloadCommand = SetupDownloadCommand(args);
            var installCommand = SetupInstallCommand(args);
            var chunkCommand = SetupChunkCommand(args);
            var unchunkCommand = SetupUnchunkCommand(args);
            var compressCommand = SetupCompressCommand(args);
            var decompressCommand = SetupDecompressCommand(args);

            var root = new RootCommand("Installer for the Common Core platform");
            root.TreatUnmatchedTokensAsErrors = true;
            root.Add(downloadCommand);
            root.Add(installCommand);
            root.Add(chunkCommand);
            root.Add(unchunkCommand);
            root.Add(compressCommand);
            root.Add(decompressCommand);

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

            var downloadConfigParam = new Option<FileInfo>("--download", "Location of download configuration");
            downloadConfigParam.IsRequired = false;
            downloadConfigParam.AddAlias("-d");
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

            var installConfigParam = new Option<FileInfo>("--install", "Location of install configuration");
            installConfigParam.IsRequired = false;
            installConfigParam.AddAlias("-i");
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

            command.SetHandler(async (source, dest, size) =>
            {
                await ExecuteChunkCommandAsync(source, dest, size, args);
            }, sourceParam, destParam, sizeParam);

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

            command.SetHandler(async (source, dest) =>
            {
                await ExecuteUnchunkCommandAsync(source, dest, args);
            }, sourceParam, destParam);

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

            command.SetHandler(async (source, dest) =>
            {
                await ExecuteCompressCommandAsync(source, dest, args);
            }, sourceParam, destParam);

            return command;
        }

        private static Command SetupDecompressCommand(string[] args)
        {
            var command = new Command("decompress", "Decompress a file back into directory structure");
            command.TreatUnmatchedTokensAsErrors = true;

            var sourceParam = new Option<FileInfo>("--source", "File to decompress");
            sourceParam.IsRequired = true;
            sourceParam.AddAlias("-s");
            command.Add(sourceParam);

            var destParam = new Option<DirectoryInfo>("--dest", "Directory write contents to");
            destParam.IsRequired = true;
            destParam.AddAlias("-d");
            command.Add(destParam);

            command.SetHandler(async (source, dest) =>
            {
                await ExecuteDecompressCommandAsync(source, dest, args);
            }, sourceParam, destParam);

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

        private static async Task ExecuteChunkCommandAsync(FileInfo source, DirectoryInfo destination, int size, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataChunker(loggerFactory);
            await chunker.ChunkFileAsync(source, destination, size);
        }

        private static async Task ExecuteUnchunkCommandAsync(DirectoryInfo source, FileInfo destination, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataChunker(loggerFactory);
            await chunker.UnchunkFileAsync(source, destination);
        }

        private static async Task ExecuteCompressCommandAsync(DirectoryInfo source, FileInfo destination, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataCompressor(loggerFactory);
            await chunker.CompressDirectoryAsync(source, destination);
        }


        private static async Task ExecuteDecompressCommandAsync(FileInfo source, DirectoryInfo destination, string[] args)
        {
            var (_, loggerFactory) = Initialize(args);

            var chunker = new DataCompressor(loggerFactory);
            await chunker.DecompressFileAsync(source, destination);
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

        private static Dictionary<string, string> BuildConfigParameters(string[] parameters)
        {
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
