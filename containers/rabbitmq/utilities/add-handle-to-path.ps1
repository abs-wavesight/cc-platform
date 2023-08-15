Function Add-PathVariable {
  param (
      [string]$AddPath
  )
  Write-Output "Adding ${AddPath} to PATH"
  if (Test-Path $AddPath){
      $Path = [Environment]::GetEnvironmentVariable("PATH", "Machine") + [IO.Path]::PathSeparator + $AddPath;
      [Environment]::SetEnvironmentVariable("Path", $Path, "Machine");
  } else {
      Throw "'$AddPath' is not a valid path."
  }
}

Write-Output "Before PATH: $([Environment]::GetEnvironmentVariable("PATH", "Machine"))"
$ScriptDirectory = $( Split-Path $MyInvocation.MyCommand.Path )
Add-PathVariable "${ScriptDirectory}\"
Write-Output "After PATH: $([Environment]::GetEnvironmentVariable("PATH", "Machine"))"
