$ErrorActionPreference = "Stop"

function Ensure-Dir {
    param([string]$Path)
    if (!(Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function GenerateCA {
    param(
        [string]$CertsDir,
        [string]$KeysDir,
        [string]$ConfigFilePath
    )
    Ensure-Dir $CertsDir
    Ensure-Dir $KeysDir
    $caKey = Join-Path $KeysDir 'ca.key.pem'
    $caCert = Join-Path $CertsDir 'ca.pem'
    if (!(Test-Path $caKey) -or !(Test-Path $caCert)) {
        Write-Output "Generating CA key and certificate"
        openssl genrsa -out $caKey 4096
        if (!(Test-Path $caKey)) {
            Write-Error "Failed to generate CA key at $caKey"
            exit 1
        }
        openssl req -x509 -new -nodes -key $caKey -sha256 -days 36500 -out $caCert -subj "/C=US/ST=TX/L=Spring/O=American Bureau of Shipping/CN=dev-root-ca" -config $ConfigFilePath
        if (!(Test-Path $caCert)) {
            Write-Error "Failed to generate CA certificate at $caCert"
            exit 1
        }
    } else {
        Write-Output "Found existing CA key & certificate"
    }
}

function GenerateSignedCert {
    param(
        [string]$CertsDir,
        [string]$KeysDir,
        [string]$FileNamePrefix,
        [string]$ConfigFilePath,
        [string]$CAKey,
        [string]$CACert
    )
    Ensure-Dir $CertsDir
    Ensure-Dir $KeysDir
    $keyPath = Join-Path $KeysDir ("${FileNamePrefix}.pem")
    $csrPath = Join-Path $CertsDir ("${FileNamePrefix}.csr.pem")
    $certPath = Join-Path $CertsDir ("${FileNamePrefix}.pem")
    if (!(Test-Path $keyPath) -or !(Test-Path $certPath)) {
        Write-Output "Generating key and CSR for $FileNamePrefix"
        openssl req -newkey rsa:2048 -nodes -keyout $keyPath -out $csrPath -config $ConfigFilePath -extensions v3_req -reqexts v3_req
        if (!(Test-Path $keyPath) -or !(Test-Path $csrPath)) {
            Write-Error "Failed to generate key or CSR for $FileNamePrefix"
            exit 1
        }
        Write-Output "Signing $FileNamePrefix certificate with CA"
        openssl x509 -req -in $csrPath -CA $CACert -CAkey $CAKey -CAcreateserial -out $certPath -days 36500 -sha256 -extfile $ConfigFilePath -extensions v3_req
        if (!(Test-Path $certPath)) {
            Write-Error "Failed to generate signed certificate for $FileNamePrefix"
            exit 1
        }
        Remove-Item $csrPath
    } else {
        Write-Output "Found existing $FileNamePrefix key & certificate"
    }
}

# Directories
$localCerts = 'C:\local-certs'
$localKeys = 'C:\local-keys'
$remoteCerts = 'C:\remote-certs'
$remoteKeys = 'C:\remote-keys'

# Generate CA and copy to both local and remote
$opensslCAConfig = 'C:\config\openssl-ca.cnf'
GenerateCA -CertsDir $localCerts -KeysDir $localKeys -ConfigFilePath $opensslCAConfig
if (Test-Path "$localCerts\ca.pem") {
    Copy-Item "$localCerts\ca.pem" "$remoteCerts\ca.pem" -Force
}
if (Test-Path "$localKeys\ca.key.pem") {
    Copy-Item "$localKeys\ca.key.pem" "$remoteKeys\ca.key.pem" -Force
}

# Generate client cert for local
GenerateSignedCert -CertsDir $localCerts -KeysDir $localKeys -FileNamePrefix 'rabbitmq' -ConfigFilePath 'C:\config\openssl.cnf' -CAKey "$localKeys\ca.key.pem" -CACert "$localCerts\ca.pem"

# Generate server cert for remote
GenerateSignedCert -CertsDir $remoteCerts -KeysDir $remoteKeys -FileNamePrefix 'rabbitmq' -ConfigFilePath 'C:\config\openssl-rabbitmq-remote.cnf' -CAKey "$remoteKeys\ca.key.pem" -CACert "$remoteCerts\ca.pem"

Write-Output "Certificate generation complete."
