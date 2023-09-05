try
{
  $SshConfigFilePath = "C:/ProgramData/ssh/sshd_config"
  $SshTempConfigFilePath = "C:/ProgramData/ssh/sshd.config"
  if (Test-Path $SshConfigFilePath)
  {
    Remove-Item $SshConfigFilePath -Verbose -Force
  }
  if (Test-Path $SshTempConfigFilePath)
  {
    Remove-Item $SshTempConfigFilePath -Verbose -Force
  }

  Write-Output "`nCopying OpenSSH config files..."
  Copy-Item "C:/config/sshd.config" -Destination "C:/ProgramData/ssh" -Force

  $drexGroupName = "drex_group"
  Write-Output "`nCreating DREX local user group..."
  & net localgroup $drexGroupName /ADD

  $Config = Get-Content -Path "C:/config/config.json" -Raw | ConvertFrom-Json
  foreach($ClientName in $Config.clients)
  {
    $username = $ClientName
    $password = $Config.defaultPassword
    & C:\\scripts\\create-drex-user.ps1 $username $password $false $false
  }

  Write-Output "`nAdding users..."
  foreach($User in $Config.users)
  {
    $username = $User.username
    $password = $User.password

    & C:\\scripts\\create-drex-user.ps1 $username $password $true $false
  }

  Write-Output "`nOpenSSH Config file contents:"
  Get-Content -Path "C:/ProgramData/ssh/sshd.config"

  Write-Output "`nRenaming OpenSSH config file..."
  $SshConfigFilePath = "C:/ProgramData/ssh/sshd_config"
  Rename-Item -Path $SshTempConfigFilePath -NewName "sshd_config"

  Write-Output "`nSSH directory contents:"
  Get-ChildItem "C:/ssh-keys/"

  Write-Output "`nCopying SSH keys..." 
  Copy-Item "C:/ssh-keys/ssh_host_rsa_key" -Destination "C:/ProgramData/ssh" -Force
  Copy-Item "C:/ssh-keys/ssh_host_rsa_key.pub" -Destination "C:/ProgramData/ssh" -Force
  
  Copy-Item "C:/ssh-keys/ssh_host_ecdsa_key" -Destination "C:/ProgramData/ssh" -Force
  Copy-Item "C:/ssh-keys/ssh_host_ecdsa_key.pub" -Destination "C:/ProgramData/ssh" -Force

  Copy-Item "C:/ssh-keys/ssh_host_ed25519_key" -Destination "C:/ProgramData/ssh" -Force
  Copy-Item "C:/ssh-keys/ssh_host_ed25519_key.pub" -Destination "C:/ProgramData/ssh" -Force

  Write-Output "Fixing host file permissions"
  C:\\openssh\\OpenSSH-Win64\\FixHostFilePermissions.ps1 -Confirm:$false

  Write-Output "Fixing user file permissions"
  & C:\\openssh\\OpenSSH-Win64\\FixUserFilePermissions.ps1 -Confirm:$false

  Write-Output "`nOpenSSH directory contents:"
  Get-ChildItem "C:/ProgramData/ssh/"

  Write-Output "`nStarting sshd..."
  Start-Service ssh-agent
  Start-Service sshd

  & "C:/working/spinner.exe" service sshd -t C:\ProgramData\ssh\logs\sshd.log
}
catch
{
  $msg = @()
  $err = $_.Exception
  do {
      $msg += $err.Message
      $err = $err.InnerException
  } while ($err)

  Write-Output "Fatal Exception: $($msg -join ' - ')"
  exit 1
}