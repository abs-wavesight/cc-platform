$ErrorActionPreference = "Stop"

function GenerateKeyAndCert() {
  param(
    [string]
    $CertsDir,

    [string]
    $KeysDir,

    [string]
    $FileNamePrefix,

    [string]
    $ConfigFilePath
  )

  if (!(Test-Path $ConfigFilePath)) {
    Write-Error "Could not find config file at $ConfigFilePath"
    exit 1
  }
  if ((Test-Path "${KeysDir}\${FileNamePrefix}.key") -and (Test-Path "${CertsDir}\${FileNamePrefix}.pem")) {
    Write-Output "Found existing key & certificate"

  } else {
    Write-Output "Generating RSA private key & X.509 certificate in ${dir}"
    openssl req -newkey rsa:2048 -nodes -keyout ${KeysDir}\${FileNamePrefix}.key -x509 -days 36500 -outform PEM -out ${CertsDir}\${FileNamePrefix}.pem -text -config $ConfigFilePath -extensions v3_req

    # We also need to convert the certificate to a PKCS#12 format (*.pfx) or DER (*.cer)
    # so it can be imported into a Windows certificate trust store.
    openssl x509 -in ${CertsDir}\${FileNamePrefix}.pem -out ${CertsDir}\${FileNamePrefix}.cer
  }
}

GenerateKeyAndCert -KeysDir C:\local-keys -CertsDir C:\local-certs -FileNamePrefix rabbitmq -ConfigFilePath "C:\config\openssl-prod.cnf"