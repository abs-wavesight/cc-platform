namespace Abs.CommonCore.Installer.Services;

public interface IReleaseDataProvider
{
    Task<IEnumerable<string>> GetReleaseNames(string owner, string repoName);
}
