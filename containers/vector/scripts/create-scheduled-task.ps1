param(
  [string]
  [ValidateScript(
      {
        $( Test-Path -Path $_ -PathType Container )
      }
  )]
  [Alias('d')]
# A relative or absolute path to a writeable directory containing log files
  $LogsDir,

  [string]
  [Alias('b')]
  $Before = "90d"
)

$ResolvedLogsDir = Resolve-Path $LogsDir
$TaskName = 'ABS Wavesight - Clean Logs'
$ExecutionTime = "06:00"
$UserPrincipalName = [Security.Principal.WindowsIdentity]::GetCurrent().Name

$TaskQuery = (schtasks /query /fo CSV /nh) | Out-String
if ($TaskQuery -match "${TaskName}")
{
  Write-Output "Found existing scheduled task ""$TaskName"". Will need to unregister & recreate."
  & schtasks /delete /tn "${TaskName}" /f
}

$ScriptDirectory = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition)
$CommandPath = "${ScriptDirectory}\clean-logs.ps1"
$FullCommand = "CMD /S /C '$PsHome\pwsh.exe' '$CommandPath' -LogsDir '$ResolvedLogsDir' -Before '$Before'"

Write-Output "Using the following parameters for scheduled task:"  
Write-Output "TaskName: ${TaskName}"
Write-Output "FullCommand: ${FullCommand}"
Write-Output "DailyExecutionTime: ${ExecutionTime}"
Write-Output "UserPrincipalName: ${UserPrincipalName}"

& schtasks /create /sc daily /tn "${TaskName}" /tr $FullCommand /st $ExecutionTime /ru $UserPrincipalName /rl HIGHEST /f