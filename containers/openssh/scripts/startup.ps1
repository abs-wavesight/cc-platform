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

  Write-Output "`nCreating DREX local user group..."
  & net localgroup "drex_group" /ADD

  $Config = Get-Content -Path "C:/config/config.json" -Raw | ConvertFrom-Json
  foreach($ClientName in $Config.clients)
  {
    Write-Output "`nCreating user (if not exists) for client $ClientName..."
    & net USER $ClientName $Config.defaultPassword /ADD && net localgroup "Administrators" $ClientName /ADD

    Write-Output "`nCreating directory for client $ClientName..."
    & mkdir C:/sftproot/$($ClientName)
    
    Write-Output "`Adding match user block for client $ClientName..."
    $MatchUserBlock = @"

Match User ${ClientName}
  ChrootDirectory c:\sftproot\${ClientName}\
  PermitTunnel no
  AllowAgentForwarding no
  AllowTcpForwarding no
  X11Forwarding no
  GatewayPorts no

"@
    Add-Content $SshTempConfigFilePath $MatchUserBlock
  }

  Write-Output "`nOpenSSH Config file contents:"
  Get-Content -Path "C:/ProgramData/ssh/sshd.config"

  Write-Output "`nRenaming OpenSSH config file..."
  $SshConfigFilePath = "C:/ProgramData/ssh/sshd_config"
  Rename-Item -Path $SshTempConfigFilePath -NewName "sshd_config"

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