using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Abs.CommonCore.LocalDevUtility.Models;
using Figgle;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility;

public static class Program
{
    public static async Task<int> Main(string[]? args)
    {
        Console.WriteLine(FiggleFonts.Doom.Render("ABS-WS | Common Core"));
        AnsiConsole.Write(new Rule("Local Dev Utility"));

        var root = BuildRootCommand();
        root.AddCommand(BuildConfigureCommand());
        root.AddCommand(BuildRunCommand());
        root.AddCommand(BuildStopCommand());
        return await root.InvokeAsync(args ?? Array.Empty<string>());
    }

    private static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("Utility to aid local development and testing");
        root.SetHandler(() =>
        {
            Console.WriteLine("You must use a sub-command to invoke this utility: \"configure\", \"run\", or \"stop\".");
        });
        return root;
    }

    private static Command BuildConfigureCommand()
    {
        var command = new Command("configure");

        var printOption = new Option<bool?>("--print-only")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        printOption.AddAlias("-p");
        command.AddOption(printOption);

        command.Handler = CommandHandler.Create(ConfigureCommand.Configure);

        return command;
    }

    private static Command BuildRunCommand()
    {
        var command = new Command("run");

        command.AddOption(GetRunComponentOption("drex-service"));
        command.AddOption(GetRunComponentOption("rabbitmq"));
        command.AddOption(GetRunComponentOption("vector"));
        command.AddOption(GetRunComponentOption("grafana"));
        command.AddOption(GetRunComponentOption("loki"));

        command.AddOption(GetFlagOption("deps", "d"));
        command.AddOption(GetFlagOption("log-viz", "l"));
        command.AddOption(GetFlagOption("reset", "r"));
        command.AddOption(GetFlagOption("background", "b"));
        command.AddOption(GetFlagOption("abort-on-container-exit", "a"));
        command.AddOption(GetFlagOption("confirm", "c"));

        var siteConfigOverrideOption = new Option<string?>("--drex-site-config-file-name-override")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        siteConfigOverrideOption.AddAlias("-s");
        command.AddOption(siteConfigOverrideOption);

        command.Handler = CommandHandler.Create(RunCommand.Run);

        return command;
    }

    private static Command BuildStopCommand()
    {
        var command = new Command("stop");
        return command;
    }

    private static Option<RunComponentMode?> GetRunComponentOption(string name)
    {
        return new Option<RunComponentMode?>($"--{name}")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private static Option<bool?> GetFlagOption(string name, string? alias = null)
    {
        var option = new Option<bool?>($"--{name}");
        if (!string.IsNullOrEmpty(alias))
        {
            option.AddAlias($"-{alias}");
        }
        return option;
    }
}
