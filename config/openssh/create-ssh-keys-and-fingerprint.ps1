param ($output_path)

New-Item -Type Directory $output_path -Force > $null
$ssh_host_rsa_key_path = @(Join-Path -Path $output_path -ChildPath "ssh_host_rsa_key")
# Generate ssh host rsa key
if (Test-Path -Path $ssh_host_rsa_key_path)
{
    Write-Output "$ssh_host_rsa_key_path already exists."
}
else 
{
    $temp = @(Join-Path -Path $output_path -ChildPath "temp/")
    $keygenOutputPath = @(Join-Path -Path $temp -ChildPath "__PROGRAMDATA__/ssh")
    New-Item -Path $keygenOutputPath -ItemType "directory" -Force
    ssh-keygen -A -f $temp

    Move-Item -Path "$keygenOutputPath\*" -Destination $output_path -Force
    Remove-Item -Path $temp -Recurse -Force
}

# Generate fingerprint
if (Test-Path -Path $ssh_host_rsa_key_path)
{
    # Default: 3072 SHA256:Wl1/wCywwvUOrs8J2++YnvKgGmiokFgDc9J8fu8Dwl0 eagle\user@APQ1mA07ItRkr3t (RSA)
    # Expected: ssh-rsa 3072 Wl1/wCywwvUOrs8J2++YnvKgGmiokFgDc9J8fu8Dwl0
    $fingerprint = @(ssh-keygen -lf $ssh_host_rsa_key_path)
    $temp = $fingerprint.split(" ");
    $len = $temp[0] # 3072
    $value = $temp[1].split(":")[1] # Wl1/wCywwvUOrs8J2++YnvKgGmiokFgDc9J8fu8Dwl0

    $fingerprint_file = @(Join-Path -Path $output_path -ChildPath "ssh-host-key-fingerprint.txt")
    "ssh-rsa $len $value" | Out-File "$fingerprint_file"
}
else
{
    Write-Output "$ssh_host_rsa_key_path not found."
    exit 1
}