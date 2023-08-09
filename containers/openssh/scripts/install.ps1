$ErrorActionPreference = 'Stop'

Push-Location C:\\openssh\\OpenSSH-Win64
Write-Output "Enable logfile"
((Get-Content -path sshd_config_default -Raw) -replace '#SyslogFacility AUTH','SyslogFacility LOCAL0') | Set-Content -Path sshd_config_default

Write-Output "Installing OpenSSH"
& .\\install-sshd.ps1

Write-Output "Generating host keys"
.\\ssh-keygen.exe -A

Write-Output "Fixing host file permissions"
& .\\FixHostFilePermissions.ps1 -Confirm:$false

Write-Output "Fixing user file permissions"
& .\\FixUserFilePermissions.ps1 -Confirm:$false

Pop-Location

$newPath = 'C:\\OpenSSH-Win64;' + [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("PATH", $newPath, [EnvironmentVariableTarget]::Machine)

Write-Output "Adding public key to authorized_keys"
$keyPath = "~\\.ssh\\authorized_keys"
New-Item -Type Directory ~\\.ssh > $null
$sshKey | Out-File $keyPath -Encoding Ascii

Write-Output "Setting sshd service restart behavior"
sc.exe failure sshd reset= 86400 actions= restart/500
