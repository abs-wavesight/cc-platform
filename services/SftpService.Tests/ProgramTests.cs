using System.Text.Json;
using Abs.CommonCore.SftpService;
using Rebex.Net;

namespace SftpService.Tests;

public class ProgramTests
{
    [Fact]
    public async Task RunApplication_GenerateKey_KeyGenerated()
    {
        var tempPath = Directory.GetCurrentDirectory();

        var returnCode = await Program.Main(new[] { "gen-key", "-p", tempPath });

        var keyPath = Path.Combine(tempPath, Program.KeyFileName);
        var key = new SshPrivateKey(keyPath);

        var fingerprintPath = Path.Combine(tempPath, Program.KeyFingerprintFileName);
        var fingerprint = await File.ReadAllTextAsync(fingerprintPath);

        var keyFingerprint = Convert.ToBase64String(key.Fingerprint.ToArray(Program.FingerprintHash));

        Assert.Equal(0, returnCode);
        Assert.Equal(keyFingerprint, fingerprint);
    }

    [Fact]
    public async Task RunApplication_AddDrexUser_UserAdded()
    {
        var configPath = Path.Combine("config", Program.ConfigFileName);

        var username = Guid.NewGuid().ToString();
        var password = Guid.NewGuid().ToString();
        var returnCode = await Program.Main(new[] { "add-user", "-u", username, "-p", password, "-d" });

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;

        var isMatch = config.Sites
                            .Any(x => x.Username == username && x.Password == password);

        Assert.Equal(0, returnCode);
        Assert.True(isMatch);
    }

    [Fact]
    public async Task RunApplication_AddDrexUserWithoutPassword_ExceptionThrown()
    {
        var username = Guid.NewGuid().ToString();
        var returnCode = await Program.Main(new[] { "add-user", "-u", username, "-d" });

        Assert.True(returnCode != 0);
    }

    [Fact]
    public async Task RunApplication_AddClientUser_UserAdded()
    {
        var configPath = Path.Combine("config", Program.ConfigFileName);

        var username = Guid.NewGuid().ToString();
        await Program.Main(new[] { "add-user", "-u", username, });

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;

        var isMatch = config.Clients
                            .Any(x => x == username);

        Assert.True(isMatch);
    }
}
