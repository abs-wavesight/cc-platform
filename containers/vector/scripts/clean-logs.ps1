param(
  [string]
  [ValidateScript(
      {
        $_ -eq "" -or $( Test-Path -Path $_ -PathType Container )
      }
  )]
  [Alias('d')]
# A relative or absolute path to a writeable directory containing log files
  $LogsDir,

  [string]
  [Alias('b')]
# Either "now", an ISO8601 date or datetime, or a relative offset in days (e.g. 90d). Default is 90 days.
  $Before = "90d"
)

function parseDateTimeStr
{
  param (
    [Parameter(Mandatory, Position = 0)]
    [string]
    $DateTimeStr
  )
  $RelativeDateTimeRegex = "^(\d+)d$" # a number of days, e.g. '90d'

  if ($DateTimeStr -imatch $RelativeDateTimeRegex)
  {
    $RegexMatch = $DateTimeStr | Select-String -Pattern $RelativeDateTimeRegex
    $NumDays = [int]$RegexMatch.Matches.Groups[1].value
    $DateTimeStr = (Get-Date).AddDays(-1*[int]$NumDays)
  }
  else
  {
    throw "Invalid datetime string provided"
  }
  return $DatetimeStr
}

function DeleteFile()
{
  # Wrapping Remove-Item in a function to force it to have a "High" impact
  # This yields more desirable default behavior of making confirmation prompts opt-out
  # (by either setting $ConfirmPreference to "None" or calling with `-Confirm:$false`),
  # since most people won't have $ConfirmPreference set to "Medium" prior to executing.
  [CmdletBinding(SupportsShouldProcess, ConfirmImpact = "High")]
  param(
    [Parameter(
        Position = 0,
        Mandatory = $true,
        ValueFromPipeline = $true)
    ]
    [System.IO.FileInfo]
    $file
  )
  Process {
    # Executes once for each pipeline object
    if ( $PSCmdlet.ShouldProcess($file.FullName, 'Delete File'))
    {
      $file | Remove-Item -WhatIf:$WhatIfPreference -Confirm:$false
    }
  }
}

$ErrorActionPreference = "Stop" # Similar to set -e in bash

# Note that default values on parameters must be literals, so we have to set the LogsDir default down here
if ($LogsDir -eq "")
{
  $DefaultLogsDir = Resolve-Path "$( Split-Path $MyInvocation.MyCommand.Path )\..\logs"
  Write-Output "Using default logs directory ""$DefaultLogsDir"""
  $ResolvedLogsDir = $DefaultLogsDir
}
else
{
  $ResolvedLogsDir = Resolve-Path $LogsDir
  Write-Output "Using resolved logs directory ""$ResolvedLogsDir"""
}


$ParsedDateTime = parseDateTimeStr $Before

Write-Output "Checking for files unchanged since $ParsedDateTime"

$FilesToDelete = Get-ChildItem $ResolvedLogsDir -filter "*.log" |
    Where-Object -FilterScript {
      ($_.LastWriteTime -lt $ParsedDateTime)
    }
if ($FilesToDelete.Length -eq 0)
{
  Write-Output "No files to delete"
}
else
{
  Write-Output "Found $( $FilesToDelete.Length ) files matching criteria"
}
$FilesToDelete | DeleteFile
