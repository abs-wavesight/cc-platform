using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Abs.CommonCore.Installer.Extensions;
using Octokit;
using Polly;

namespace Abs.CommonCore.Installer.Services;

[ExcludeFromCodeCoverage]
public class DataRequestService : IDataRequestService
{
    private readonly ILogger _logger;
    private readonly string? _nugetEnvironmentVariable;
    private readonly bool _verifyOnly;

    private readonly HttpClient _httpClient = new();

    public DataRequestService(ILoggerFactory loggerFactory, bool verifyOnly = false)
    {
        _logger = loggerFactory.CreateLogger<DataRequestService>();
        _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);
        _verifyOnly = verifyOnly;

        if (verifyOnly == false && string.IsNullOrWhiteSpace(_nugetEnvironmentVariable))
        {
            throw new Exception("Nuget environment variable not found");
        }
    }

    public async Task<Stream> RequestByteArrayAsync(string source)
    {
        _logger.LogInformation($"Loading data from '{source}'");

        return _verifyOnly
            ? Stream.Null
            : source.Contains("github.com", StringComparison.OrdinalIgnoreCase) && source.Contains("/releases/", StringComparison.OrdinalIgnoreCase)
            ? await DownloadGithubReleaseFileAsync(source)
            : source.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            ? await DownloadGithubRawFileAsync(source)
            : await DownloadDirectFileAsync(source);
    }

    private async Task<Stream> DownloadGithubReleaseFileAsync(string source)
    {
        var client = new GitHubClient(new Octokit.ProductHeaderValue(Constants.AbsHeaderValue))
        {
            Credentials = new Credentials(_nugetEnvironmentVariable)
        };

        var segments = source.GetGitHubPathSegments();
        var release = await client.Repository.Release.Get(segments.Owner, segments.Repo, segments.Tag);
        var asset = release.Assets
            .First(x => string.Equals(x.Name, segments.File));

        var uri = new Uri(asset.Url, UriKind.Absolute);
        var response = await client.Connection.Get<Stream>(uri, new Dictionary<string, string>(), "application/octet-stream");

        if (!response.HttpResponse.IsSuccessStatusCode())
        {
            var message = $"Response status code does not indicate success: {response.HttpResponse.StatusCode}.";
            throw new HttpRequestException(message);
        }

        if (response.Body is null)
        {
            return Stream.Null;
        }

        MemoryStream ms = new();
        await response.Body.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    private Task<Stream> DownloadGithubRawFileAsync(string source)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(3))
            .ExecuteAsync(() => DownloadFileAsync(source, true));
    }

    private Task<Stream> DownloadDirectFileAsync(string source)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2))
            .ExecuteAsync(() => DownloadFileAsync(source, false));
    }

    private async Task<Stream> DownloadFileAsync(string source, bool isGithub)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, source);
        if (isGithub)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nugetEnvironmentVariable);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));
        }

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        MemoryStream ms = new();
        await response.Content.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }
}
