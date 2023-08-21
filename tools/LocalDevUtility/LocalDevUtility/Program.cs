using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Commands.Shared;
using Abs.CommonCore.LocalDevUtility.Commands.Stop;
using Abs.CommonCore.LocalDevUtility.Commands.TestDrex;
using Abs.CommonCore.LocalDevUtility.Extensions;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.Platform.Extensions;
using Figgle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility;

public class Program
{
    public static async Task<int> Main(string[]? args = null)
    {
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss.fff ";
                });
            });
        services.AddSingleton<IPowerShellAdapter, PowerShellAdapter>();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var powerShellAdapter = serviceProvider.GetRequiredService<IPowerShellAdapter>();

        return await Run(args, logger, powerShellAdapter);
    }

    public static async Task<int> Run(string[]? args, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        Console.WriteLine(FiggleFonts.Doom.Render("ABS-WS | Common Core"));
        AnsiConsole.Write(new Rule("Local Dev Utility"));

        var root = BuildRootCommand();
        root.AddCommand(BuildConfigureCommand(logger, powerShellAdapter));
        root.AddCommand(BuildRunCommand(logger, powerShellAdapter));
        root.AddCommand(BuildStopCommand(logger, powerShellAdapter));
        root.AddCommand(BuildTestDrexCommand(logger, powerShellAdapter));
        return await root.InvokeAsync(args ?? Array.Empty<string>());
    }

    private static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("Utility to aid local development and testing");
        root.SetHandler(() =>
        {
            Console.WriteLine("You must use a sub-command to invoke this utility: \"configure\", \"run\", \"test-drex\" or \"stop\".");
        });
        return root;
    }

    private static Command BuildConfigureCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("configure", "Configure the utility via interactive prompts. Must be run at least once before using \"run\".");

        var printOption = new Option<bool?>("--print-only", "Print the current configuration without prompting for new values")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        printOption.AddAlias("-p");
        command.AddOption(printOption);

        command.Handler = CommandHandler.Create(async (ConfigureOptions configureOptions) =>
        {
            return await TryExecuteCommandAsync(
                async () => await ConfigureCommand.Configure(configureOptions, logger, powerShellAdapter),
                logger);
        });

        return command;
    }

    private static Command BuildRunCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("run", "Run Common Core components via Docker");

        var modeOption = new Option<RunMode?>("--mode", "r = run immediately, c = confirm before running, o = output and copy to clipboard without running (default = r)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        modeOption.AddAlias("-m");
        command.AddOption(modeOption);

        AddComponentOptions(command);

        command.AddOption(GetFlagOption("reset", "r", "Reset Docker"));
        command.AddOption(GetFlagOption("background", "b", "Run in background, a.k.a. detached (cannot be used with --abort-on-container-exit)"));
        command.AddOption(GetFlagOption("abort-on-container-exit", "a", "Abort if any container exits (cannot be used with --background)"));
        command.AddOption(GetFlagOption("verbose", "v", "Print out final compose configuration"));

        var siteConfigOverrideOption = new Option<string?>("--drex-site-config-file-name-override")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        siteConfigOverrideOption.AddAlias("-s");
        command.AddOption(siteConfigOverrideOption);

        command.Handler = CommandHandler.Create(async (RunOptions runOptions) =>
        {
            return await TryExecuteCommandAsync(
                async () => await RunCommand.Run(runOptions, logger, powerShellAdapter),
                logger);
        });

        return command;
    }

    private static Command BuildTestDrexCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("test-drex", "Facilitates running the DREX Test Client");

        var roleOption = new Option<Role?>(new[] { "--role", "-r" },
            description: "\"producer\"/\"p\" or \"consumer\"/\"c\"; if this is not provided, \"--config\" parameter must be present");
        command.AddOption(roleOption);

        var originOption = new Option<Origin?>(new[] { "--origin", "-o" },
            description: "\"site\"/\"s\" or \"central\"/\"c\"");
        command.AddOption(originOption);

        var loopOption = new Option<bool?>(new[] { "--loop", "-l" },
            description: "If present the producer will run continuously, producing 1 message every second until stopped");
        command.Add(loopOption);

        var configOption = new Option<FileInfo?>(new[] { "--config", "-c" },
            description: "Override absolute path to an alternative config file; if this is not provided, \"--role\" parameter must be present");
        command.Add(configOption);

        command.Handler = CommandHandler.Create(async (TestDrexOptions configureOptions) =>
        {
            return await TryExecuteCommandAsync(
                async () => await TestDrexCommand.Run(configureOptions, logger, powerShellAdapter),
                logger);
        });

        return command;
    }

    private static Command BuildStopCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("stop", "Stop compose components that may have been started in the background");

        AddComponentOptions(command);

        command.AddOption(GetFlagOption("reset", "r", "Reset Docker"));

        command.Handler = CommandHandler.Create((StopOptions stopOptions) =>
        {
            return TryExecuteCommandAsync(
                async () => await StopCommand.Stop(stopOptions, logger, powerShellAdapter),
                logger);
        });

        return command;
    }

    private static void AddComponentOptions(Command command)
    {
        foreach (var runComponentPropertyName in ComposeOptions.ComponentPropertyNames)
        {
            var parameterName = runComponentPropertyName.ToSnakeCase();
            var description = typeof(RunOptions).GetDescription(runComponentPropertyName);
            command.AddOption(GetRunComponentOption(parameterName, description));
        }
    }

    private static async Task<int> TryExecuteCommandAsync(Func<Task<int>> command, ILogger logger)
    {
        try
        {
            return await command();
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while running the command:\n{ex}");
            return 1;
        }
    }

    private static Option<ComposeComponentMode> GetRunComponentOption(string name, string? description = null)
    {
        var option = new Option<ComposeComponentMode>($"--{name}", $"Component: i = from image, s = from source{(string.IsNullOrWhiteSpace(description) ? "" : $"; {description}")}")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        option.AddValidator(optionResult =>
        {
            if (!optionResult.Tokens.Any())
            {
                optionResult.ErrorMessage = $"Invalid value for \"{optionResult.Token}\". You must provide a valid component mode (i = from image, s = from source).";
            }
        });
        return option;
    }

    private static Option<bool?> GetFlagOption(string name, string alias, string description)
    {
        var option = new Option<bool?>($"--{name}", description)
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        option.AddAlias($"-{alias}");
        return option;
    }
}
