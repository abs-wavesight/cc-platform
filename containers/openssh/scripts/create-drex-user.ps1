param (
  [string]$Username="drex",
  [string]$Password="P@ssword"
)

$DrexGroupName="drex_group"

Write-Output "Creating DREX user '$Username'..."
& net localgroup $DrexGroupName /ADD
& net user $Username $Password /ADD && 
& net localgroup "Administrators" $Username /ADD
& net localgroup $DrexGroupName $Username /ADD
Write-Output "Done."
