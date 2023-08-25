using System.Text.RegularExpressions;
using Abs.CommonCore.Installer.Services;

namespace Abs.CommonCore.Installer.Actions;

public partial class ContainerVersionProvider : ActionBase
{
    [GeneratedRegex("^windows-(2019|2022)-main-[0-9a-z]{7}$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ContainerTagRegex();

    private readonly IContainerTagProvider _tagProvider;

    public ContainerVersionProvider(IContainerTagProvider tagProvider)
    {
        _tagProvider = tagProvider;
    }

    public async Task<string?> GetLatestContainerVersionAsync(string containerName, string owner)
    {
        // Looking for the latest tag like "windows-2019-main-absc123"
        var tag = (await _tagProvider.GetContainerTagsAsync(containerName, owner))
            .FirstOrDefault(tag => ContainerTagRegex().IsMatch(tag));
        var sha = tag?[^7..];

        return sha;
    }
}
