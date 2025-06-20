﻿using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Config;
using Abs.CommonCore.Platform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Abs.CommonCore.Installer;

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
        var uninstallCommand = SetupUninstallCommand(args);
        var getReleaseVersionCommand = SetupGetVersionCommand(args);
        var getContainerVersionCommand = SetupGetContainerVersionCommand(args);
        var addSftpUserCommand = SetupAddSftpUserCommand(args);
        var addRestoreCommand = SetupRestoreCommand(args);

        var root = new RootCommand("Installer for the Common Core platform")
        {
            TreatUnmatchedTokensAsErrors = true
        };
        root.Add(downloadCommand);
        root.Add(installCommand);
        root.Add(chunkCommand);
        root.Add(unchunkCommand);
        root.Add(compressCommand);
        root.Add(uncompressCommand);
        root.Add(releaseBodyCommand);
        root.Add(configureRabbitCommand);
        root.Add(uninstallCommand);
        root.Add(getReleaseVersionCommand);
        root.Add(getContainerVersionCommand);
        root.Add(addSftpUserCommand);
        root.Add(addRestoreCommand);

        var result = await root.InvokeAsync(args);
        await Task.Delay(1000);
        return result;
    }

    private static Command SetupAddSftpUserCommand(string[] args)
    {
        const string commandName = "add-sftp-user";
        var command = new Command(commandName)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        const string usernameParamName = "--name";
        var usernameParam = new Option<string>(usernameParamName)
        {
            IsRequired = true
        };
        command.Add(usernameParam);

        const string isDrexParamName = "--drex";
        var isDrexParam = new Option<bool>(isDrexParamName);
        command.Add(isDrexParam);

        command.SetHandler(Handle, usernameParam, isDrexParam);

        return command;

        Task<int> Handle(string name, bool isDrex)
        {
            return ExecuteAddSftpUserCommand(name, isDrex, args);
        }
    }

    private static Command SetupDownloadCommand(string[] args)
    {
        var command = new Command("download", "Download components for installation")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration")
        {
            IsRequired = true
        };
        registryParam.SetDefaultValue(new FileInfo("SystemRegistryConfig.json"));
        registryParam.AddAlias("-r");
        command.Add(registryParam);

        var downloadConfigParam = new Option<FileInfo>("--config", "Location of download configuration")
        {
            IsRequired = false
        };
        downloadConfigParam.AddAlias("-dc");
        command.Add(downloadConfigParam);

        var componentParam = new Option<string[]>("--component", "Specific component to process")
        {
            IsRequired = false
        };
        componentParam.AddAlias("-c");
        componentParam.AllowMultipleArgumentsPerToken = true;
        command.Add(componentParam);

        var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter")
        {
            IsRequired = false
        };
        parameterParam.AddAlias("-p");
        parameterParam.AllowMultipleArgumentsPerToken = true;
        command.Add(parameterParam);

        var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
        verifyOnlyParam.SetDefaultValue(false);
        verifyOnlyParam.IsRequired = false;
        verifyOnlyParam.AddAlias("-v");
        command.Add(verifyOnlyParam);

        var noPromptParam = new Option<bool>("--no-prompt", "Indicates to not prompt for missing parameters");
        noPromptParam.SetDefaultValue(false);
        noPromptParam.IsRequired = false;
        noPromptParam.AddAlias("-np");
        command.Add(noPromptParam);

        command.SetHandler(Handle, registryParam, downloadConfigParam, componentParam, parameterParam, verifyOnlyParam, noPromptParam);

        return command;

        Task<int> Handle(FileInfo registryConfig, FileInfo? downloaderConfig, string[] components, string[] parameters, bool verifyOnly, bool noPrompt)
        {
            return ExecuteDownloadCommandAsync(registryConfig, downloaderConfig, components, parameters, verifyOnly, noPrompt, args);
        }
    }

    private static Command SetupInstallCommand(string[] args)
    {
        var command = new Command("install", "Install components")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration")
        {
            IsRequired = true
        };
        registryParam.SetDefaultValue(new FileInfo("SystemRegistryConfig.json"));
        registryParam.AddAlias("-r");
        command.Add(registryParam);

        var installConfigParam = new Option<FileInfo>("--config", "Location of install configuration")
        {
            IsRequired = false
        };
        installConfigParam.AddAlias("-ic");
        command.Add(installConfigParam);

        var componentParam = new Option<string[]>("--component", "Specific component to process")
        {
            IsRequired = false
        };
        componentParam.AddAlias("-c");
        componentParam.AllowMultipleArgumentsPerToken = true;
        command.Add(componentParam);

        var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter")
        {
            IsRequired = false
        };
        parameterParam.AddAlias("-p");
        parameterParam.AllowMultipleArgumentsPerToken = true;
        command.Add(parameterParam);

        var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
        verifyOnlyParam.SetDefaultValue(false);
        verifyOnlyParam.IsRequired = false;
        verifyOnlyParam.AddAlias("-v");
        command.Add(verifyOnlyParam);

        var noPromptParam = new Option<bool>("--no-prompt", "Indicates to not prompt for missing parameters");
        noPromptParam.SetDefaultValue(false);
        noPromptParam.IsRequired = false;
        noPromptParam.AddAlias("-np");
        command.Add(noPromptParam);

        command.SetHandler(Handle, registryParam, installConfigParam, componentParam, parameterParam, verifyOnlyParam, noPromptParam);

        return command;

        Task<int> Handle(FileInfo registryConfig, FileInfo installerConfig, string[] components, string[] parameters, bool verifyOnly, bool noPrompt)
        {
            return ExecuteInstallCommandAsync(registryConfig, installerConfig, components, parameters, verifyOnly,
                noPrompt, args);
        }
    }

    private static Command SetupChunkCommand(string[] args)
    {
        var command = new Command("chunk", "Break a file into multiple smaller pieces")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var sourceParam = new Option<FileInfo>("--source", "File to separate into chunks")
        {
            IsRequired = true
        };
        sourceParam.AddAlias("-s");
        command.Add(sourceParam);

        var destParam = new Option<DirectoryInfo>("--dest", "Directory to store pieces in")
        {
            IsRequired = true
        };
        destParam.AddAlias("-d");
        command.Add(destParam);

        var sizeParam = new Option<int>("--size", "Size in bytes to limit chunks to")
        {
            IsRequired = true
        };
        sizeParam.AddAlias("-sz");
        command.Add(sizeParam);

        var removeSourceParam = new Option<bool>("--remove-source", "Remove source file after processing");
        removeSourceParam.SetDefaultValue(false);
        removeSourceParam.IsRequired = false;
        removeSourceParam.AddAlias("-rs");
        command.Add(removeSourceParam);

        var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.")
        {
            IsRequired = false
        };
        configParam.AddAlias("-c");
        command.Add(configParam);

        command.SetHandler(Handle, sourceParam, destParam, sizeParam, removeSourceParam, configParam);
        return command;

        Task<int> Handle(FileInfo source, DirectoryInfo dest, int size, bool removeSource, FileInfo config)
        {
            return ExecuteChunkCommandAsync(source, dest, size, removeSource, config, args);
        }
    }

    private static Command SetupUnchunkCommand(string[] args)
    {
        var command = new Command("unchunk", "Merges file chunks into a single file")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var sourceParam = new Option<DirectoryInfo>("--source", "Directory where file chunks are")
        {
            IsRequired = true
        };
        sourceParam.AddAlias("-s");
        command.Add(sourceParam);

        var destParam = new Option<FileInfo>("--dest", "File to write combined pieces to")
        {
            IsRequired = true
        };
        destParam.AddAlias("-d");
        command.Add(destParam);

        var removeSourceParam = new Option<bool>("--remove-source", "Remove source directory after processing");
        removeSourceParam.SetDefaultValue(false);
        removeSourceParam.IsRequired = false;
        removeSourceParam.AddAlias("-rs");
        command.Add(removeSourceParam);

        var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.")
        {
            IsRequired = false
        };
        configParam.AddAlias("-c");
        command.Add(configParam);

        command.SetHandler(Handle, sourceParam, destParam, removeSourceParam, configParam);

        return command;

        Task<int> Handle(DirectoryInfo source, FileInfo dest, bool removeSource, FileInfo config)
        {
            return ExecuteUnchunkCommandAsync(source, dest, removeSource, config, args);
        }
    }

    private static Command SetupCompressCommand(string[] args)
    {
        var command = new Command("compress", "Compresses a directory into a single file")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var sourceParam = new Option<DirectoryInfo>("--source", "Directory to compress")
        {
            IsRequired = true
        };
        sourceParam.AddAlias("-s");
        command.Add(sourceParam);

        var destParam = new Option<FileInfo>("--dest", "File to write compressed contents to")
        {
            IsRequired = true
        };
        destParam.AddAlias("-d");
        command.Add(destParam);

        var removeSourceParam = new Option<bool>("--remove-source", "Remove source directory after processing");
        removeSourceParam.SetDefaultValue(false);
        removeSourceParam.IsRequired = false;
        removeSourceParam.AddAlias("-rs");
        command.Add(removeSourceParam);

        var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.")
        {
            IsRequired = false
        };
        configParam.AddAlias("-c");
        command.Add(configParam);

        command.SetHandler(Handle, sourceParam, destParam, removeSourceParam, configParam);

        return command;

        Task<int> Handle(DirectoryInfo source, FileInfo dest, bool removeSource, FileInfo config)
        {
            return ExecuteCompressCommandAsync(source, dest, removeSource, config, args);
        }
    }

    private static Command SetupUncompressCommand(string[] args)
    {
        var command = new Command("uncompress", "Uncompresses a file back into directory structure")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var sourceParam = new Option<FileInfo>("--source", "File to uncompress")
        {
            IsRequired = true
        };
        sourceParam.AddAlias("-s");
        command.Add(sourceParam);

        var destParam = new Option<DirectoryInfo>("--dest", "Directory to write contents to")
        {
            IsRequired = true
        };
        destParam.AddAlias("-d");
        command.Add(destParam);

        var removeSourceParam = new Option<bool>("--remove-source", "Remove source file after processing");
        removeSourceParam.SetDefaultValue(false);
        removeSourceParam.IsRequired = false;
        removeSourceParam.AddAlias("-rs");
        command.Add(removeSourceParam);

        var configParam = new Option<FileInfo>("--config", $"Location of download configuration. `{ComponentNamePlaceholder}` in source/destination is replaced with config component.")
        {
            IsRequired = false
        };
        configParam.AddAlias("-c");
        command.Add(configParam);

        command.SetHandler(Handle, sourceParam, destParam, removeSourceParam, configParam);

        return command;

        Task<int> Handle(FileInfo source, DirectoryInfo dest, bool removeSource, FileInfo config)
        {
            return ExecuteUncompressCommandAsync(source, dest, removeSource, config, args);
        }
    }

    private static Command SetupReleaseBodyCommand(string[] args)
    {
        var command = new Command("release-body", "Builds a release body based on configuration")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var configParam = new Option<FileInfo>("--config", $"Location of download configuration.")
        {
            IsRequired = false
        };
        configParam.AddAlias("-c");
        command.Add(configParam);

        var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter")
        {
            IsRequired = false
        };
        parameterParam.AddAlias("-p");
        parameterParam.AllowMultipleArgumentsPerToken = true;
        command.Add(parameterParam);

        var outputParam = new Option<FileInfo>("--output", "Location to save release body to")
        {
            IsRequired = true
        };
        outputParam.AddAlias("-o");
        command.Add(outputParam);

        command.SetHandler(Handle, configParam, parameterParam, outputParam);

        return command;

        Task<int> Handle(FileInfo? config, string[]? parameters, FileInfo output)
        {
            return ExecuteReleaseBodyBuilderCommandAsync(config, parameters, output, args);
        }
    }

    private static Command SetupConfigureRabbitCommand(string[] args)
    {
        var command = new Command("configure-rabbit", "Configures rabbit mq with credentials for configuration file")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var rabbitParam = new Option<Uri>("--rabbit", $"Http uri to connect to rabbit mq")
        {
            IsRequired = true
        };
        rabbitParam.AddAlias("-r");
        command.Add(rabbitParam);

        var rabbitUsernameParam = new Option<string>("--rabbit-user", "Username to connect to rabbit")
        {
            IsRequired = true
        };
        rabbitUsernameParam.AddAlias("-ru");
        command.Add(rabbitUsernameParam);

        var rabbitPasswordParam = new Option<string>("--rabbit-password", "Password to connect to rabbit")
        {
            IsRequired = true
        };
        rabbitPasswordParam.AddAlias("-rp");
        command.Add(rabbitPasswordParam);

        var usernameParam = new Option<string>("--user", "Username to use for account")
        {
            IsRequired = true
        };
        usernameParam.AddAlias("-u");
        command.Add(usernameParam);

        var passwordParam = new Option<string>("--password", "Password to use for account")
        {
            IsRequired = false
        };
        passwordParam.AddAlias("-p");
        command.Add(passwordParam);

        var updatePermissionsParam = new Option<bool>("--update-permissions", "Update the users permissions")
        {
            IsRequired = false
        };
        updatePermissionsParam.AddAlias("-up");
        command.Add(updatePermissionsParam);

        var drexSiteConfigParam = new Option<FileInfo>("--drex-site-config", "Path to drex site config to update")
        {
            IsRequired = false
        };
        drexSiteConfigParam.AddAlias("-dsc");
        command.Add(drexSiteConfigParam);

        var accountTypeParam = new Option<AccountType>("--type", "Indicates the type of the account to create")
        {
            IsRequired = true
        };
        accountTypeParam.AddAlias("-t");
        command.Add(accountTypeParam);

        var credentialsFileParam = new Option<FileInfo>("--credentials-file", "Updates the file with the generated credentials")
        {
            IsRequired = false
        };
        credentialsFileParam.AddAlias("-cf");
        command.Add(credentialsFileParam);

        var isSilentParam = new Option<bool>("--silent", "Indicates whether the operation is silent")
        {
            IsRequired = false
        };
        isSilentParam.AddAlias("-s");
        command.Add(isSilentParam);

        command.SetHandler(Handle);

        return command;

        Task<int> Handle(InvocationContext context)
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
                AccountType = context.ParseResult.GetValueForOption(accountTypeParam),
                CredentialsFile = context.ParseResult.GetValueForOption(credentialsFileParam),
                IsSilent = context.ParseResult.GetValueForOption(isSilentParam),
            };

            return ExecuteConfigureRabbitCommandAsync(arguments, args);
        }
    }

    private static Command SetupUninstallCommand(string[] args)
    {
        var command = new Command("uninstall", "Uninstalls all installed components")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var dockerParam = new Option<DirectoryInfo>("--docker", "Path to docker location")
        {
            IsRequired = false
        };
        dockerParam.AddAlias("-d");
        command.Add(dockerParam);

        var pathParam = new Option<DirectoryInfo>("--path", "Path to the installation location")
        {
            IsRequired = false
        };
        pathParam.AddAlias("-p");
        command.Add(pathParam);

        var removeSystemParam = new Option<bool>("--remove-system", "Indicates to remove system components")
        {
            IsRequired = false
        };
        removeSystemParam.SetDefaultValue(false);
        command.Add(removeSystemParam);

        var removeConfigParam = new Option<bool>("--remove-config", "Indicates to remove configuration files")
        {
            IsRequired = false
        };

        removeConfigParam.SetDefaultValue(false);
        command.Add(removeConfigParam);

        var removeDockerParam = new Option<bool>("--remove-docker", "Indicates to remove docker")
        {
            IsRequired = false
        };
        removeDockerParam.SetDefaultValue(false);
        command.Add(removeDockerParam);

        command.SetHandler(Handle, dockerParam, pathParam, removeSystemParam, removeConfigParam, removeDockerParam);

        return command;

        Task<int> Handle(DirectoryInfo dockerLocation, DirectoryInfo installPath, bool removeSystem, bool removeConfig, bool removeDocker)
        {
            return ExecuteUninstallCommandAsync(dockerLocation, installPath, removeSystem, removeConfig, removeDocker, args);
        }
    }

    private static Command SetupGetVersionCommand(string[] args)
    {
        const string commandName = "get-release-version";
        var command = new Command(commandName)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        const string releaseNameParamName = "--name";
        var releaseNameParam = new Option<string>(releaseNameParamName)
        {
            IsRequired = true
        };
        command.Add(releaseNameParam);

        const string ownerParamName = "--owner";
        var ownerParam = new Option<string>(ownerParamName);
        releaseNameParam.IsRequired = true;
        command.Add(ownerParam);

        const string repoParamName = "--repo";
        var repoParam = new Option<string>(repoParamName)
        {
            IsRequired = true
        };
        command.Add(repoParam);

        command.SetHandler(async (releaseName, owner, repo) =>
        {
            var result = await ExecuteGetReleaseVersionCommandAsync(releaseName, owner, repo, args);
            await Console.Out.WriteAsync(result);
        }, releaseNameParam, ownerParam, repoParam);

        return command;
    }

    private static Command SetupGetContainerVersionCommand(string[] args)
    {
        const string commandName = "get-container-version";
        var command = new Command(commandName)
        {
            TreatUnmatchedTokensAsErrors = true
        };

        const string releaseNameParamName = "--name";
        var releaseNameParam = new Option<string>(releaseNameParamName)
        {
            IsRequired = true
        };
        command.Add(releaseNameParam);

        const string ownerParamName = "--owner";
        var ownerParam = new Option<string>(ownerParamName);
        releaseNameParam.IsRequired = true;
        command.Add(ownerParam);

        command.SetHandler(async (releaseName, owner) =>
        {
            var result = await ExecuteGetContainerVersionCommandAsync(releaseName, owner, args);
            await Console.Out.WriteAsync(result);
        }, releaseNameParam, ownerParam);

        return command;
    }

    private static Command SetupRestoreCommand(string[] args)
    {
        var command = new Command("restore", "Restore installed components")
        {
            TreatUnmatchedTokensAsErrors = true
        };

        var registryParam = new Option<FileInfo>("--registry", "Location of registry configuration")
        {
            IsRequired = true
        };
        registryParam.SetDefaultValue(new FileInfo("SystemRegistryConfig.json"));
        registryParam.AddAlias("-r");
        command.Add(registryParam);

        var installConfigParam = new Option<FileInfo>("--config", "Location of install configuration")
        {
            IsRequired = false
        };
        installConfigParam.AddAlias("-ic");
        command.Add(installConfigParam);

        var composePathParam = new Option<string>("--compose-path", "Path to compose files")
        {
            IsRequired = true
        };
        composePathParam.AddAlias("-cp");
        composePathParam.SetDefaultValue("$ABS_PATH\\config");
        command.Add(composePathParam);

        var parameterParam = new Option<string[]>("--parameter", "Specific colon separated key value pair to use as a config parameter")
        {
            IsRequired = false
        };
        parameterParam.AddAlias("-p");
        parameterParam.AllowMultipleArgumentsPerToken = true;
        command.Add(parameterParam);

        var verifyOnlyParam = new Option<bool>("--verify", "Verify actions without making any changes");
        verifyOnlyParam.SetDefaultValue(false);
        verifyOnlyParam.IsRequired = false;
        verifyOnlyParam.AddAlias("-v");
        command.Add(verifyOnlyParam);

        var noPromptParam = new Option<bool>("--no-prompt", "Indicates to not prompt for missing parameters");
        noPromptParam.SetDefaultValue(false);
        noPromptParam.IsRequired = false;
        noPromptParam.AddAlias("-np");
        command.Add(noPromptParam);

        command.SetHandler(Handle, registryParam, installConfigParam, composePathParam, parameterParam, verifyOnlyParam, noPromptParam);

        return command;

        Task<int> Handle(FileInfo registryConfig, FileInfo? installerConfig, string composePath, string[] parameters, bool verifyOnly, bool noPrompt)
        {
            return ExecuteRestoreCommandAsync(
                registryConfig,
                installerConfig,
                composePath,
                parameters,
                verifyOnly,
                noPrompt, args);
        }
    }

    private static async Task<int> ExecuteDownloadCommandAsync(FileInfo registryConfig, FileInfo? downloaderConfig, string[] components, string[] parameters, bool verifyOnly, bool noPrompt, string[] args)
    {
        var config = new FileInfo("SystemConfig.json");
        if (downloaderConfig == null && config.Exists)
        {
            downloaderConfig = config;
        }

        var configParameters = BuildConfigParameters(parameters);
        var filePath = BuildDownloadLogFileLocation(registryConfig, downloaderConfig, configParameters);
        var (_, loggerFactory) = Initialize(filePath, args);
        var promptForMissingParameters = !noPrompt;

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var dataRequest = new DataRequestService(loggerFactory, verifyOnly);
            var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
            var downloader = new ComponentDownloader(loggerFactory, dataRequest, commandExecution, registryConfig, downloaderConfig, configParameters, promptForMissingParameters);
            await downloader.ExecuteAsync(components);
        }, true);
    }

    private static async Task<int> ExecuteInstallCommandAsync(FileInfo registryConfig, FileInfo? installerConfig, string[] components, string[] parameters, bool verifyOnly, bool noPrompt, string[] args)
    {
        var config = new FileInfo("SystemConfig.json");
        if (installerConfig == null && config.Exists)
        {
            installerConfig = config;
        }

        var configParameters = BuildConfigParameters(parameters);
        var filePath = BuildInstallLogFileLocation(registryConfig, installerConfig, configParameters);
        var (logger, loggerFactory) = Initialize(filePath, args);
        var promptForMissingParameters = !noPrompt;

        var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
        var serviceManager = new WindowsServiceManager(logger, commandExecution);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var installer = new ComponentInstaller(loggerFactory, commandExecution, serviceManager, registryConfig, installerConfig, configParameters, promptForMissingParameters);
            await installer.ExecuteAsync(components);
        }, true);
    }

    private static async Task<int> ExecuteChunkCommandAsync(FileInfo source, DirectoryInfo destination, int size, bool removeSource, FileInfo? config, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var chunker = new DataChunker(loggerFactory);
            return await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) => await chunker.ChunkFileAsync(s, d, size, removeSource));
        });
    }

    private static async Task<int> ExecuteUnchunkCommandAsync(DirectoryInfo source, FileInfo destination, bool removeSource, FileInfo? config, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var chunker = new DataChunker(loggerFactory);
            return await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) => await chunker.UnchunkFileAsync(s, d, removeSource));
        });
    }

    private static async Task<int> ExecuteCompressCommandAsync(DirectoryInfo source, FileInfo destination, bool removeSource, FileInfo? config, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var compressor = new DataCompressor(loggerFactory);
            return await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) => await compressor.CompressDirectoryAsync(s, d, removeSource));
        });
    }

    private static async Task<int> ExecuteUncompressCommandAsync(FileInfo source, DirectoryInfo destination, bool removeSource, FileInfo? config, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var compressor = new DataCompressor(loggerFactory);
            return await ExecuteForComponentsAsync(source, destination, config,
                async (s, d) => await compressor.UncompressFileAsync(s, d, removeSource));
        });
    }

    private static async Task<int> ExecuteReleaseBodyBuilderCommandAsync(FileInfo? config, string[]? parameters, FileInfo output, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);
        var configParameters = BuildConfigParameters(parameters);

        return await ExecuteCommandAsync(loggerFactory,
            async () => await ReleaseBodyBuilder.BuildReleaseBodyAsync(config, configParameters, output));
    }

    private static async Task<int> ExecuteConfigureRabbitCommandAsync(RabbitConfigureCommandArguments arguments, string[] args)
    {
        var (logger, loggerFactory) = Initialize(args);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            if (arguments.AccountType == AccountType.Unknown)
            {
                logger.LogError("Account type must be specified");
                return Constants.ExitCodes.GENERIC_ERROR;
            }

            if (arguments.UpdatePermissions)
            {
                await RabbitConfigurer.UpdateUserPermissionsAsync(arguments.Rabbit!, arguments.RabbitUsername!, arguments.RabbitPassword!, arguments.Username!, arguments.AccountType);
                return Constants.ExitCodes.SUCCESS;
            }

            var credentials = await RabbitConfigurer.ConfigureRabbitAsync(arguments.Rabbit!, arguments.RabbitUsername!, arguments.RabbitPassword!, arguments.Username!, arguments.Password, arguments.AccountType, arguments.IsSilent);

            if (credentials == null)
            {
                logger.LogWarning("No credentials added");
                return Constants.ExitCodes.SUCCESS;
            }

            if (arguments.DrexSiteConfig != null)
            {
                try
                {
                    await RabbitConfigurer.UpdateDrexSiteConfigAsync(arguments.DrexSiteConfig, credentials);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating drex site config");
                    return Constants.ExitCodes.GENERIC_ERROR;
                }
            }

            if (arguments.CredentialsFile != null)
            {
                try
                {
                    await RabbitConfigurer.UpdateCredentialsFileAsync(credentials, arguments.CredentialsFile);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating credentials file");
                    return Constants.ExitCodes.GENERIC_ERROR;
                }
            }

            return Constants.ExitCodes.SUCCESS;
        });
    }

    private static async Task<int> ExecuteUninstallCommandAsync(DirectoryInfo? dockerLocation, DirectoryInfo? installPath, bool? removeSystem, bool? removeConfig, bool? removeDocker, string[] args)
    {
        var (logger, loggerFactory) = Initialize(args);
        var commandExecution = new CommandExecutionService(loggerFactory);
        var serviceManager = new WindowsServiceManager(logger, commandExecution);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var uninstaller = new Uninstaller(loggerFactory, commandExecution, serviceManager);
            await uninstaller.UninstallSystemAsync(dockerLocation, installPath, removeSystem, removeConfig, removeDocker);
        });
    }

    private static async Task<int> ExecuteRestoreCommandAsync(FileInfo registryConfig, FileInfo? installerConfig, string composePath, string[] parameters, bool verifyOnly, bool noPrompt, string[] args)
    {
        var configParameters = BuildConfigParameters(parameters);

        var config = new FileInfo("SystemConfig.json");
        if (installerConfig == null && config.Exists)
        {
            installerConfig = config;
        }

        composePath = composePath.ReplaceConfigParameters(configParameters);

        var filePath = BuildInstallLogFileLocation(registryConfig, installerConfig, configParameters, "restore");
        var (logger, loggerFactory) = Initialize(filePath, args);
        var promptForMissingParameters = !noPrompt;

        var commandExecution = new CommandExecutionService(loggerFactory, verifyOnly);
        var serviceManager = new WindowsServiceManager(logger, commandExecution);

        var component = new Component
        {
            Name = "Direct",
            Actions = new List<ComponentAction>(),
            Files = new List<ComponentFile>(),
            AdditionalProperties = new Dictionary<string, object>()
        };

        var action = new ComponentAction
        {
            Action = ComponentActionAction.SystemRestore,
            Source = composePath.ReplaceConfigParameters(configParameters),
            Destination = "",
            AdditionalProperties = new Dictionary<string, object>()
        };

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var installer = new ComponentInstaller(loggerFactory, commandExecution, serviceManager, registryConfig, installerConfig, configParameters, promptForMissingParameters);
            await installer.RunSystemRestoreCommandAsync(component, action.Source, action);
        }, true);
    }

    private static async Task<int> ExecuteCommandAsync(ILoggerFactory loggerFactory,
        Func<Task> action, bool logError = false)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (logError)
            {
                LogCommandExecutionError(loggerFactory, ex);
            }

            return Constants.ExitCodes.GENERIC_ERROR;
        }

        return Constants.ExitCodes.SUCCESS;
    }

    private static void LogCommandExecutionError(ILoggerFactory loggerFactory, Exception? ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();

        do
        {
            logger.LogError(ex, "Error executing command");
            ex = ex?.InnerException;
        } while (ex is not null);
    }

    private static async Task<int> ExecuteCommandAsync(ILoggerFactory loggerFactory,
        Func<Task<int>> action, bool logError = false)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (logError)
            {
                LogCommandExecutionError(loggerFactory, ex);
            }

            return Constants.ExitCodes.GENERIC_ERROR;
        }
    }

    private static async Task<int> ExecuteForComponentsAsync(FileInfo source, DirectoryInfo destination, FileInfo? config, Func<FileInfo, DirectoryInfo, Task> action)
    {
        if (config == null)
        {
            await action(source, destination);
            return Constants.ExitCodes.SUCCESS;
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

        return Constants.ExitCodes.SUCCESS;
    }

    private static async Task<int> ExecuteForComponentsAsync(DirectoryInfo source, FileInfo destination, FileInfo? config, Func<DirectoryInfo, FileInfo, Task> action)
    {
        if (config == null)
        {
            await action(source, destination);
            return Constants.ExitCodes.SUCCESS;
        }

        var downloaderConfig = ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(config.FullName);
        var components = downloaderConfig.Components;

        await components
            .ForAllAsync(async c =>
            {
                var s = source.FullName.Replace(ComponentNamePlaceholder, c, StringComparison.OrdinalIgnoreCase);
                var d = destination.FullName.Replace(ComponentNamePlaceholder, c,
                    StringComparison.OrdinalIgnoreCase);

                await action(new DirectoryInfo(s), new FileInfo(d));
            });

        return Constants.ExitCodes.SUCCESS;
    }

    private static async Task<int> ExecuteAddSftpUserCommand(string name, bool isDrex, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);
        var commandExecution = new CommandExecutionService(loggerFactory);

        return await ExecuteCommandAsync(loggerFactory, async () =>
        {
            var creator = new AddSftpUser(commandExecution);
            await creator.AddSftpUserAsync(name, isDrex);
        });
    }

    private static async Task<string?> ExecuteGetReleaseVersionCommandAsync(string releaseName, string owner, string repoName, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);
        var releaseDataProvider = new ReleaseDataProvider();
        var versionProvider = new ReleaseVersionProvider(releaseDataProvider);

        string? version = null;
        await ExecuteCommandAsync(loggerFactory, async () => version = await versionProvider.GetVersionAsync(releaseName, owner, repoName));

        return version;
    }

    private static async Task<string?> ExecuteGetContainerVersionCommandAsync(string releaseName, string owner, string[] args)
    {
        var (_, loggerFactory) = Initialize(args);
        var tagProvider = new ContainerTagProvider();
        var versionProvider = new ContainerVersionProvider(tagProvider);

        string? version = null;
        await ExecuteCommandAsync(loggerFactory, async () => version = await versionProvider.GetLatestContainerVersionAsync(releaseName, owner));

        return version;
    }

    private static (ILogger, ILoggerFactory) Initialize(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        return ConfigureLogging(builder.Logging);
    }

    private static (ILogger, ILoggerFactory) Initialize(string filePath, string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        return ConfigureLogging(builder.Logging, filePath);
    }

    private static (ILogger, ILoggerFactory) ConfigureLogging(ILoggingBuilder builder, string filePath = "")
    {
        builder.ClearProviders();

        builder.AddConsole(options =>
        {
            options.FormatterName = nameof(CustomConsoleFormatter);
        });
        builder.Services.AddSingleton<ConsoleFormatter, CustomConsoleFormatter>();

        if (string.IsNullOrWhiteSpace(filePath) == false)
        {
            builder.AddFile(filePath);
        }

        var provider = builder.Services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<Program>>();
        builder.Services.AddSingleton<ILogger>(logger);

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        return (logger, loggerFactory);
    }

    private static string BuildInstallLogFileLocation(FileInfo registryConfig, FileInfo? installerConfig, Dictionary<string, string> configParameters, string suffix = "install")
    {
        var config = installerConfig != null
            ? ConfigParser.LoadConfig<InstallerComponentInstallerConfig>(installerConfig.FullName)
            : null;

        configParameters
            .MergeParameters(config?.Parameters);

        return BuildLogFileLocation(registryConfig, configParameters, suffix);
    }

    private static string BuildDownloadLogFileLocation(FileInfo registryConfig, FileInfo? downloaderConfig, Dictionary<string, string> configParameters)
    {
        var config = downloaderConfig != null
            ? ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(downloaderConfig.FullName)
            : null;

        configParameters
            .MergeParameters(config?.Parameters);

        return BuildLogFileLocation(registryConfig, configParameters, "download");
    }

    private static string BuildLogFileLocation(FileInfo registryConfig, Dictionary<string, string> configParameters, string name)
    {
        var registry = ConfigParser.LoadConfig<InstallerComponentRegistryConfig>(registryConfig.FullName);
        var root = registry.Location
            .ReplaceConfigParameters(configParameters);

        Directory.CreateDirectory(root);
        var fileName = $"{{Date}}{DateTime.Now:HHmmss}.{name}.log";

        return Path.Combine(root, fileName);
    }

    private static Dictionary<string, string> BuildConfigParameters(string[]? parameters)
    {
        return parameters == null
            ? new Dictionary<string, string>()
            : parameters
            .Select(x =>
            {
                var parts = x.Split(new char[] { ':' }, 2);
                return new KeyValuePair<string, string>(parts[0], parts[1]);
            })
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
