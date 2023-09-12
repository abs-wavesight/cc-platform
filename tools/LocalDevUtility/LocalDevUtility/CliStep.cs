namespace Abs.CommonCore.LocalDevUtility;

public class CliStep : IDisposable
{
    public static CliStep Start(string description, bool multiLine = false)
    {
        var toPrint = $"\n{description}... ";
        if (multiLine)
        {
            Console.WriteLine(toPrint);
        }
        else
        {
            Console.Write(toPrint);
        }

        return new CliStep();
    }

    public void Dispose()
    {
        Console.WriteLine("Done.");
    }
}
