namespace Abs.CommonCore.LocalDevUtility.Models;

public enum RunMode
{
    /// <summary>Runs the docker compose command immediately</summary>
    r,
    /// <summary>Confirms before running the command</summary>
    c,
    /// <summary>Only outputs the command (and copies to clipboard)</summary>
    o
}
