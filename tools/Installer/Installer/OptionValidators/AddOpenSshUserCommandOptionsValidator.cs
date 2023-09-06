using System.CommandLine.Parsing;
using System.Text.RegularExpressions;

namespace Abs.CommonCore.Installer.OptionValidators;
public partial class AddOpenSshUserCommandOptionsValidator
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9]{4,255}$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex UsernameRegex();

    public static void ValidateUserName(OptionResult symbolResult)
    {
        var username = symbolResult.Tokens.FirstOrDefault()?.Value;
        if (username == null)
        {
            var errorMessage = $"{symbolResult.Option.Name} is required.";
            symbolResult.ErrorMessage = errorMessage;
            return;
        }

        if (!UsernameRegex().IsMatch(username))
        {
            var errorMessage = $"'{username}' is not valid.";
            symbolResult.ErrorMessage = errorMessage;
        }
    }
}
