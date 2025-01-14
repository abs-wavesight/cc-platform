Function Add-PathVariable {
  param (
      [string]$AddPath
  )
  Write-Output "Adding ${AddPath} to PATH"
  if (Test-Path $AddPath){
      $RegexAddPath = [regex]::Escape($AddPath)
      $ArrPath = $env:Path -split ';' | Where-Object {$_ -notMatch "^$RegexAddPath\\?"}
      $env:Path = ($ArrPath + $AddPath) -join ';'
      Set-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Environment' -Name PATH -Value $env:Path
  } else {
      Throw "'$AddPath' is not a valid path."
  }
}

Write-Output "Before PATH: ${Env:Path}"
$ScriptDirectory = $( Split-Path $MyInvocation.MyCommand.Path )
Add-PathVariable "${ScriptDirectory}\"
Write-Output "After PATH: ${Env:Path}"
