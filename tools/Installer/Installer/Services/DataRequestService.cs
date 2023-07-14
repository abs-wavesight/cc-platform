using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Services
{
    [ExcludeFromCodeCoverage]
    public class DataRequestService : IDataRequestService
    {
        private readonly ILogger _logger;
        private const string _nugetEnvironmentVariableName = "ABS_NUGET_PASSWORD";
        private readonly string? _nugetEnvironmentVariable;
        private readonly bool _verifyOnly;

        private readonly HttpClient _httpClient = new HttpClient();

        public DataRequestService(ILoggerFactory loggerFactory, bool verifyOnly = false)
        {
            _logger = loggerFactory.CreateLogger<DataRequestService>();
            _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(_nugetEnvironmentVariableName);
            _verifyOnly = verifyOnly;

            if (verifyOnly == false && string.IsNullOrWhiteSpace(_nugetEnvironmentVariable))
            {
                throw new Exception("Nuget environment variable not found");
            }
        }

        public async Task<byte[]> RequestByteArrayAsync(string source)
        {
            _logger.LogInformation($"Loading data from '{source}'");

            if (_verifyOnly)
            {
                return Array.Empty<byte>();
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, source))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nugetEnvironmentVariable);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));

                using (var response = await _httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
        }
    }
}
