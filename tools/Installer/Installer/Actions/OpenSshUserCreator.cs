using Abs.CommonCore.Installer.Services;
using PasswordGenerator;

namespace Abs.CommonCore.Installer.Actions;
public class OpenSshUserCreator
{
    private readonly ICommandExecutionService _commandExecutionService;

    public OpenSshUserCreator(ICommandExecutionService commandExecutionService)
    {
        _commandExecutionService = commandExecutionService;
    }

    public async Task AddOpenSshUserAsync(string name, bool isDrex)
    {
        var pwd = new Password();
        var password = pwd.Next();

        const string containerName = "openssh";
        var dockerCommand = $"exec -it {containerName} cmd.exe /C ";
        dockerCommand += "pwsh -Command C:\\scripts\\create-drex-user.ps1";
        dockerCommand += $" -Username {name}";
        if (isDrex)
        {
            dockerCommand += $" -Password \"{password}\"";
        }

        dockerCommand += $" -DrexUser {(isDrex ? 1 : 0)} -UpdateConfig 1";
        await _commandExecutionService.ExecuteCommandAsync("docker", dockerCommand, string.Empty);

        if (!isDrex)
        {
            Console.WriteLine($"Restarting {containerName} container...");
            const string restartCommand = $"restart {containerName}";
            await _commandExecutionService.ExecuteCommandAsync("docker", restartCommand, string.Empty);
        }
    }
}
