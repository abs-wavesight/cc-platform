namespace Abs.CommonCore.Installer.Services;

public class ReleaseDataProvider : GitHubClientUserBase, IReleaseDataProvider
{
    public async Task<IEnumerable<string>> GetReleaseNames(string owner, string repoName)
    {
        var client = GetClient();

        return (await client.Repository.Release.GetAll(owner, repoName))
            .OrderByDescending(r => r.PublishedAt)
            .Select(r => r.Name);
    }
}
