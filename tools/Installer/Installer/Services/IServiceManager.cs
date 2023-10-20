namespace Abs.CommonCore.Installer.Services;
public interface IServiceManager
{
    Task StopServiceAsync(string name);
    Task StartServiceAsync(string name);
    Task DeleteServiceAsync(string name, bool retry);
}
