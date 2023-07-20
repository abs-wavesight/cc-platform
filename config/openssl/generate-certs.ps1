$ErrorActionPreference = "Stop"

function GenerateKeyAndCert() {
  param(
    [string]
    $CertsDir,

    [string]
    $KeysDir,

    [string]
    $FileNamePrefix
  )

  $openSSLConfigPath = "C:\config\openssl.cnf"
  if (!(Test-Path $openSSLConfigPath)) {
    Write-Error "Could not find C:\config\openssl.cnf config file"
    exit 1
  }
  if ((Test-Path "${KeysDir}\${FileNamePrefix}.key") -and (Test-Path "${CertsDir}\${FileNamePrefix}.pem")) {
    Write-Output "Found existing key & certificate"
    openssl x509 -in ${CertsDir}\${FileNamePrefix}.pem -noout -text
  } else {
    Write-Output "Generating RSA private key & X.509 certificate in ${dir}"
    openssl req -newkey rsa:2048 -nodes -keyout ${KeysDir}\${FileNamePrefix}.key -x509 -days 36500 -outform PEM -out ${CertsDir}\${FileNamePrefix}.pem -text -config $openSSLConfigPath

    # We also need to convert the certificate to a PKCS#12 format (*.pfx) or DER (*.cer)
    # so it can be imported into a Windows certificate trust store.

    openssl x509 -in ${CertsDir}\${FileNamePrefix}.pem -out ${CertsDir}\${FileNamePrefix}.cer

  }
}

GenerateKeyAndCert -KeysDir C:\local-keys -CertsDir C:\local-certs -FileNamePrefix rabbitmq
GenerateKeyAndCert -KeysDir C:\remote-keys -CertsDir C:\remote-certs -FileNamePrefix rabbitmq

if (!(Test-Path C:\local-certs\cacerts.pem) -or !(Test-Path C:\remote-certs\cacerts.pem)) {
  Write-Output "Creating certificate authority list with generated certificates"
  Get-Content C:\local-certs\rabbitmq.pem, C:\remote-certs\rabbitmq.pem | Out-File C:\local-certs\cacerts.pem
  Get-Content C:\local-certs\rabbitmq.pem, C:\remote-certs\rabbitmq.pem | Out-File C:\remote-certs\cacerts.pem
}

Write-Output "Contents of certificate directories:"
Get-ChildItem C:\local-keys, C:\local-certs, C:\remote-keys, C:\remote-certs
