using Abs.CommonCore.Installer.Services;

namespace Abs.CommonCore.Installer.Actions;

public class ReleaseVersionProvider : ActionBase
{
    private readonly IReleaseDataProvider _releaseDataProvider;

    public ReleaseVersionProvider(IReleaseDataProvider releaseDataProvider)
    {
        _releaseDataProvider = releaseDataProvider;
    }

    public async Task<string?> GetVersionAsync(string releaseName, string owner, string repoName)
    {
        var releaseVersion = (await _releaseDataProvider.GetReleaseNames(owner, repoName))
            .FirstOrDefault(n => n.EndsWith(releaseName, StringComparison.InvariantCultureIgnoreCase))
            ?.Replace(releaseName, "")
            .Trim();

        return releaseVersion;
    }
}
