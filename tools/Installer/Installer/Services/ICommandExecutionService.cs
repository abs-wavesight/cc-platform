namespace Abs.CommonCore.Installer.Services;

public interface ICommandExecutionService
{
    Task ExecuteCommandAsync(string command, string arguments, string workingDirectory, bool throwOnError = true);

    List<string> ExecuteCommandWithResult(string command, string arguments, string workingDirectory);
}
