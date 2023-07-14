namespace Abs.CommonCore.Installer.Services
{
    public interface ICommandExecutionService
    {
        Task ExecuteCommandAsync(string command, string arguments);
    }
}
