#New-SelfSignedCertificate [-AlternateSignatureAlgorithm] [-CertStoreLocation <String>] [-CloneCert <Certificate>] [-Container
#<System.String>] [-CurveExport {None | CurveParameters | CurveName}] [-DnsName <String[]>] [-ExistingKey] [-Extension
#<System.Security.Cryptography.X509Certificates.X509Extension[]>] [-FriendlyName <System.String>] [-HardwareKeyUsage
#<Microsoft.CertificateServices.Commands.HardwareKeyUsage[]>] [-HashAlgorithm <System.String>] [-KeyAlgorithm <System.String>]
#[-KeyDescription <System.String>] [-KeyExportPolicy <Microsoft.CertificateServices.Commands.KeyExportPolicy[]>] [-KeyFriendlyName
#<System.String>] [-KeyLength <System.Int32>] [-KeyLocation <System.String>] [-KeyProtection
#<Microsoft.CertificateServices.Commands.KeyProtection[]>] [-KeySpec {None | KeyExchange | Signature}] [-KeyUsage
#<Microsoft.CertificateServices.Commands.KeyUsage[]>] [-KeyUsageProperty <Microsoft.CertificateServices.Commands.KeyUsageProperty[]>]
#[-NotAfter <System.DateTime>] [-NotBefore <System.DateTime>] [-Pin <System.Security.SecureString>] [-Provider <System.String>] [-Reader
#<System.String>] [-SecurityDescriptor <System.Security.AccessControl.FileSecurity>] [-SerialNumber <System.String>] [-Signer
#<Microsoft.CertificateServices.Commands.Certificate>] [-SignerPin <System.Security.SecureString>] [-SignerReader <System.String>]
#[-SmimeCapabilities] [-Subject <System.String>] [-SuppressOid <System.String[]>] [-TestRoot] [-TextExtension <System.String[]>] [-Type
#{Custom | CodeSigningCert | DocumentEncryptionCert | SSLServerAuthentication | DocumentEncryptionCertLegacyCsp}] [-Confirm] [-WhatIf]
#[<CommonParameters>]


# DnsName = [...] # used for SAN

#$params = @{
#  Subject = 'rabbitmq-local'
#  KeyAlgorithm = 'RSA'
#  KeyLength = 2048
#  FriendlyName = 'rabbitmq-local'
#  KeyDescription = "private key for rabbitmq TLS"
#  KeyExportPolicy = 'Exportable'
#  KeySpec = 'KeyExchange'
#  NotAfter = Get-Date -Year 2050
#  Type = 'SSLServerAuthentication'
#  WhatIf = $true
#}
#
##New-SelfSignedCertificate @params
#$mypwd = ConvertTo-SecureString -String '1234' -Force -AsPlainText
#New-SelfSignedCertificate -Subject 'rabbitmq-local' -KeyAlgorithm 'RSA' -KeyLength 2048 -FriendlyName 'rabbitmq-local' -KeyExportPolicy 'Exportable' -KeySpec 'KeyExchange' -NotAfter (Get-Date).AddYears(25) -Type 'SSLServerAuthentication' -DnsName 'rabbitmq-local' -CertStoreLocation Cert:\LocalMachine\My -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" | Export-PfxCertificate -FilePath .\my-cert.pfx -ChainOption 'BuildChain' -Password $mypwd
#$pfxData = Get-PfxData -FilePath .\my-cert.pfx -Password $mypwd
#Write-Output $pfxData
#$pfxData | Format-Table
#$pfxData.EndEntityCertificates | Format-Table
## $cert = Get-ChildItem -Path Cert:\CurrentUser\My\EEDEF61D4FF6EDBAAD538BB08CCAADDC3EE28FF
## Export-Certificate -Type CERT -FilePath .\my-cert.cer


# requires openssl to be installed
#$ScriptDirectory = $( Split-Path $MyInvocation.MyCommand.Path )
#openssl req -newkey rsa:2048 -nodes -keyout $ScriptDirectory\..\..\config\rabbitmq\domain.key -x509 -days 365 -outform PEM -out $ScriptDirectory\..\..\config\rabbitmq\domain.pem -config $ScriptDirectory\openssl.cnf
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

    # openssl pkcs12 -export -nodes -out ${CertsDir}\${FileNamePrefix}.pfx -inkey ${KeysDir}\${FileNamePrefix}.key -in ${CertsDir}\${FileNamePrefix}.pem
    openssl x509 -in ${CertsDir}\${FileNamePrefix}.pem -out ${CertsDir}\${FileNamePrefix}.cer
    # Import-Certificate -FilePath $certsDir\${FileNamePrefix}.cer -CertStoreLocation Cert:\CurrentUser\Root # TODO migrate to client apps
  }
#  return openssl x509 -in "${CertsDir}\${FileNamePrefix}.pem"
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
