using System.Text.Json.Serialization;

namespace Abs.CommonCore.Installer.Actions.Models;
public class CloudParameters
{
    [JsonPropertyName("cloudTenantId")]
    public string CloudTenantId { get; set; } = string.Empty;

    [JsonPropertyName("ccTenantId")]
    public string CcTenantId { get; set; } = string.Empty;

    [JsonPropertyName("apimServiceUrl")]
    public string ApimServiceUrl { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("cloudClientId")]
    public string CloudClientId { get; set; } = string.Empty;

    [JsonPropertyName("cloudClientSecret")]
    public string CloudClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("centralHostName")]
    public string CentralHostName { get; set; } = string.Empty;
}
