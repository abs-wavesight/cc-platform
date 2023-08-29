$ProvisionPath = "${Env:PROVISION_PATH}";
$AbsPath = "${Env:ABS_PATH}";

$InstallerPath = "${ProvisionPath}/Installer";
$AdditionalFilesPath = "${InstallerPath}/AdditionalFiles";
$RabbitMqDefaultUsername = "drex";
$RabbitMqDefaultPassword = "drex";
$RabbitMqDrexUsername = "drex";

# Create Installer directory
New-Item -ItemType Directory -Force -Path $InstallerPath;

# Copy installer and its configs
Copy-Item -Path "${ProvisionPath}/Installer.exe" -Destination "${InstallerPath}/Installer.exe";
Copy-Item -Path "${ProvisionPath}/SystemConfig.json" -Destination "${InstallerPath}/SystemConfig.json";
Copy-Item -Path "${ProvisionPath}/SystemRegistryConfig.json" -Destination "${InstallerPath}/SystemRegistryConfig.json";

# Create AdditionalFiles directory
New-Item -ItemType Directory -Force -Path $AdditionalFilesPath;

# Copy Central RabbitMQ TLS certs to AdditionalFiles
Copy-Item -Path "${ProvisionPath}/certs/central-rabbitmq.cer" -Destination "${AdditionalFilesPath}/central-rabbitmq.cer";
Copy-Item -Path "${ProvisionPath}/certs/central-rabbitmq.pem" -Destination "${AdditionalFilesPath}/central-rabbitmq.pem";

# Copy Client configs to AdditionalFiles
Get-ChildItem -Path $ProvisionPath -Filter "*.client-app-config.json" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination "${AdditionalFilesPath}/${_.Name}";
}

# Copy Site config to AdditionalFiles
Copy-Item -Path "${ProvisionPath}/site-config.json" -Destination "${AdditionalFilesPath}/site-config.json";

# Execute installer
& Installer.exe install

# Create DREX RabbitMQ user account
& Installer.exe configure-rabbit `
  -r "https://rabbitmq-local:15671" `
  -ru $RabbitMqDefaultUsername `
  -rp $RabbitMqDefaultPassword `
  -u $RabbitMqDrexUsername `
  -su `
  -cf "${AbsPath}/config/environment.env";

# TODO RH: Restart all docker services and confirm that they come back up as healthy


# TODO RH: Things we need to do AFTER VM instance creation
# 1. Update Site config for each Site to give it a unique key
# 2. Create Central RabbitMQ DREX account for each Site
# 3. Generate valid Central RabbitMQ TLS certs, install them on Central and every Site