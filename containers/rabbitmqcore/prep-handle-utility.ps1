$ScriptDirectory = $( Split-Path $MyInvocation.MyCommand.Path )

# Install PS2EXE module
Set-PSRepository PSGallery -InstallationPolicy Trusted
Install-Module ps2exe -Force -AllowClobber -Scope AllUsers
Get-Module ps2exe -ListAvailable

# Build exe
Import-Module ps2exe
& Invoke-ps2exe $ScriptDirectory/handle-mimic.ps1 $ScriptDirectory/utilities/handle.exe

Write-Output "Contents of ${ScriptDirectory}/utilities:"
Get-ChildItem $ScriptDirectory/utilities -Name