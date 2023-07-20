# RUN Invoke-WebRequest https://github.com/PowerShell/Win32-OpenSSH/releases/download/v9.2.2.0p1-Beta/OpenSSH-Win64-v9.2.2.0.msi -OutFile openssh.msi
# RUN msiexec /i openssh.msi /passive /L openssh-install-output.log
# Using a powershell image for zip extraction (during build) & HTTP healthcheck (during runtime)
# Value for windows server 2022: lts-7.2-nanoserver-ltsc2022
ARG POWERSHELL_NANOSERVER_TAG=lts-7.2-nanoserver-1809
FROM mcr.microsoft.com/powershell:${POWERSHELL_NANOSERVER_TAG} as unzip
SHELL [ "pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';" ]
WORKDIR C:/app
RUN Invoke-WebRequest -UseBasicParsing -Uri https://indy.fulgan.com/SSL/openssl-1.0.2u-x64_86-win64.zip -OutFile openssl.zip ; \
    Expand-Archive -LiteralPath openssl.zip -DestinationPath . ; \
    Remove-Item "openssl.zip,*.txt"
USER containeradministrator
RUN setx /M PATH $($Env:PATH + ';C:\app\') # add openssl directory to PATH variable
USER containeruser

#FROM mcr.microsoft.com/powershell:${POWERSHELL_NANOSERVER_TAG} AS openssl
#COPY --from=unzip --chown=containeruser:containeruser [ "C:/app/openssl.exe", "C:/Program Files/loki/loki.exe" ]
#WORKDIR "C:/Program Files/loki"
## Application won't start without configuration file, so adding default config.
## ".yaml" extension is necessary (versus ".yml") for loki to find it
#ADD [ "./loki-default-config.yml", "./config.yaml" ]

