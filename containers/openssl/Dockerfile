# Value for windows server 2022: lts-7.2-nanoserver-ltsc2022
ARG POWERSHELL_NANOSERVER_TAG=lts-7.2-nanoserver-1809
FROM mcr.microsoft.com/powershell:${POWERSHELL_NANOSERVER_TAG} as unzip
SHELL [ "pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';" ]
WORKDIR C:/app
RUN Invoke-WebRequest -UseBasicParsing -Uri https://download.firedaemon.com/FireDaemon-OpenSSL/openssl-3.1.3.zip -OutFile openssl.zip ; \
    Expand-Archive -LiteralPath openssl.zip -DestinationPath .\temp ; \
    Copy-Item -Path temp\openssl-3\x64\bin\* -Destination . ; \	
    Remove-Item "openssl.zip" ; \
	Remove-Item "temp"
USER containeradministrator
RUN setx /M PATH $($Env:PATH + ';C:\app\') # add openssl directory to PATH variable
USER containeruser
