using System.Collections.Concurrent;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility.Helpers;

public class PowerShellAdapter : IPowerShellAdapter
{
    private readonly ConcurrentDictionary<string, string> _colorsByContainerName = new();

    public List<string> RunPowerShellCommand(string command, TimeSpan? timeout)
    {
        return RunPowerShellCommand(command, null, timeout);
    }

    public List<string> RunPowerShellCommand(string command, ILogger? logger, TimeSpan? timeout)
    {
        using var ps = PowerShell.Create(RunspaceMode.NewRunspace);
        ps.AddScript(command);
        ps.AddCommand("Out-String").AddParameter("Stream", true);

        var rawOutput = new List<string>();
        var output = new PSDataCollection<string>();
        output.DataAdded += (sender, args) => ProcessCommandOutput(rawOutput, logger, sender, args);
        ps.Streams.Error.DataAdded += (sender, args) => ProcessCommandOutput(rawOutput, logger, sender, args);

        var task = ps.InvokeAsync<object, string>(null, output);
        //if (timeout is not null)
        //{
        //    task = task
        //        .WaitAsync(timeout.Value);
        //}

        task
            .GetAwaiter()
            .GetResult();

        return rawOutput;
    }

    private void ProcessCommandOutput(List<string> rawOutput, ILogger? logger, object? sender, DataAddedEventArgs eventArgs)
    {
        string? outputItem;
        switch (sender)
        {
            case PSDataCollection<string> collection:
                outputItem = collection[eventArgs.Index];
                break;
            case PSDataCollection<ErrorRecord> errorCollection:
                outputItem = errorCollection[eventArgs.Index].ToString();
                break;
            default:
                return;
        }

        OutputWithColorForDockerCompose(outputItem!);
        rawOutput.Add(outputItem!);

        if (logger != null)
        {
            logger.LogInformation(outputItem);
        }
    }

    /// <summary>
    /// Hack to get back colors-by-container in docker compose output
    /// </summary>
    /// <param name="rawOutput"></param>
    private void OutputWithColorForDockerCompose(string rawOutput)
    {
        var colorStack = new Stack<string>();
        colorStack.Push("fuchsia");
        colorStack.Push("blue");
        colorStack.Push("aqua");
        colorStack.Push("lime");
        colorStack.Push("yellow");
        colorStack.Push("teal");
        colorStack.Push("purple");
        colorStack.Push("navy");
        colorStack.Push("olive");
        colorStack.Push("green");

        // Regex to extract the starting string including the container name from the following: "my-container-name_1  | the log message goes here"
        var containerNameRegex = new Regex(@"[\w-]+?\s+\|\s+");
        var match = containerNameRegex.Match(rawOutput);
        if (match.Success)
        {
            var containerName = match.Value.Replace("|", "").Trim();
            string? color = null;
            if (_colorsByContainerName.TryGetValue(containerName, out var value))
            {
                color = value;
            }
            else if (colorStack.Any())
            {
                do
                {
                    color = colorStack.Pop();
                } while (_colorsByContainerName.Any(_ => _.Value == color));
                _colorsByContainerName.TryAdd(containerName, color);
            }

            var coloredOutputPart = $"[{color}]{match.Value}[/]";
            var uncoloredOutputPart = rawOutput.Substring(match.Value.Length).EscapeMarkup();
            AnsiConsole.MarkupLine($"{coloredOutputPart}{uncoloredOutputPart}");
            return;
        }

        Console.WriteLine(rawOutput);
    }
}
