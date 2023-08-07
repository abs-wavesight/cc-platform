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
