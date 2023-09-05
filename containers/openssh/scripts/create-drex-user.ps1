param (
  [string]$Username,
  [string]$Password = "",
  [bool]$DrexUser = $true,
  [bool]$UpdateConfig = $true
)

$configFile = "C:/config/config.json"
$config = Get-Content -Path $configFile -Raw | ConvertFrom-Json

if ($Password=$false)
{
  $Password = $config.defaultPassword
}

Write-Output "`nCreating user $Username (if not exists)..."
if ($DrexUser)
{
  $DrexGroupName="drex_group"
  & net localgroup $DrexGroupName /ADD
  & net user $Username $Password /ADD &&
  & net localgroup $DrexGroupName $Username /ADD

  Write-Output "`nCreating directory for user $Username..."
  New-Item -Path "C:/sftproot" -Name $Username -ItemType "directory" -Force

  if ($UpdateConfig)
  {
    Write-Output "Adding user to the config file.."

    $user = $config.users | Where-Object { $_.username -eq "$Username" }
    if ($null -ne $user)
    {
        $config.users | % {if($_.username -eq "$Username"){$_.password="ads1111"}}
    }
    else
    {
        $config.users += @{"username"="$Username";"password"="$Password"}
    }

    $config | ConvertTo-Json | set-content $configFile
  }
}
else
{
  & net USER $Username $Password /ADD && net localgroup "Administrators" $Username /ADD

  Write-Output "`nCreating directory for client $Username..."
  New-Item -Path "C:/sftproot" -Name $Username -ItemType "directory" -Force
  
  Write-Output "`Adding match user block for client $Username..."
  $MatchUserBlock = @"

Match User ${Username}
ChrootDirectory c:\sftproot\${Username}\
PermitTunnel no
AllowAgentForwarding no
AllowTcpForwarding no
X11Forwarding no
GatewayPorts no

"@

  $SshTempConfigFilePath = "C:/ProgramData/ssh/sshd.config"
  Add-Content $SshTempConfigFilePath $MatchUserBlock

  if ($UpdateConfig)
  {
    Write-Output "Adding user to the config file.."

    $user = $config.clients | Where-Object { $_ -eq "$Username" }
    if ($null -eq $user)
    {
      $config.clients += "$Username"
      $config | ConvertTo-Json | set-content $configFile
    }
  }
}

Write-Output "Done."
