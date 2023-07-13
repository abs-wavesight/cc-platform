namespace Abs.CommonCore.LocalDevUtility.Helpers;

public interface IPowerShellAdapter
{
    List<string> RunPowerShellCommand(string command);
}
