using Octokit;

namespace Abs.CommonCore.Installer.Services;

public class ContainerTagProvider : GitHubClientUserBase, IContainerTagProvider
{
    public async Task<IEnumerable<string>> GetContainerTagsAsync(string containerName, string owner)
    {
        var client = GetClient();

        return (await client.Packages.PackageVersions.GetAllForOrg(owner, PackageType.Container, containerName))
            .OrderByDescending(v => v.CreatedAt)
            .SelectMany(v => v.Metadata.Container.Tags);
    }
}
