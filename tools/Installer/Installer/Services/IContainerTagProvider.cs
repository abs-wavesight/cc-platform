namespace Abs.CommonCore.Installer.Services;

public interface IContainerTagProvider
{
    Task<IEnumerable<string>> GetContainerTagsAsync(string containerName, string owner);
}
