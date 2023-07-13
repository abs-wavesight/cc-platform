using System.Management.Automation;

namespace Abs.CommonCore.LocalDevUtility.Helpers;

public static class PowerShellHelper
{
    public static void RunPowerShellCommand(string command)
    {
        using var ps = PowerShell.Create();
        ps.AddScript(command);
        ps.AddCommand("Out-String").AddParameter("Stream", true);

        var output = new PSDataCollection<string>();
        output.DataAdded += ProcessCommandStandardOutput;
        ps.Streams.Error.DataAdded += ProcessCommandErrorOutput;

        var asyncToken = ps.BeginInvoke<object, string>(null, output);

        if (asyncToken.AsyncWaitHandle.WaitOne())
        {
            ps.EndInvoke(asyncToken);
        }

        ps.InvokeAsync();
    }

    private static void ProcessCommandStandardOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        Console.WriteLine(outputItem);
    }

    private static void ProcessCommandErrorOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        Console.Error.WriteLine(outputItem);
    }
}
