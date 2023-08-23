namespace Abs.CommonCore.LocalDevUtility.Helpers;

public interface IPowerShellAdapter
{
    Task<List<string>> RunPowerShellCommandAsync(string command, TimeSpan? timeout = null);
}
