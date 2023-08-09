ARG SERVER_VERSION='ltsc2019'
ARG POWERSHELL_NANOSERVER_TAG='lts-7.2-nanoserver-1809'

# FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION}
FROM mcr.microsoft.com/powershell:$POWERSHELL_NANOSERVER_TAG AS openssh
ENV OPENSSH_VERSION 9.2.2.0p1-Beta
ENV SPINNER_VERSION 1.0.8
USER containeradministrator
WORKDIR C:/working
SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; \
    Invoke-WebRequest $('https://github.com/PowerShell/Win32-OpenSSH/releases/download/v{0}/OpenSSH-Win64.zip' -f $env:OPENSSH_VERSION) -OutFile openssh.zip -UseBasicParsing; \
    Expand-Archive openssh.zip C:\\openssh\\; \
    Remove-Item openssh.zip;

RUN [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 ; \
    Invoke-WebRequest $('https://github.com/ticketmaster/spinner/releases/download/v{0}/spinner_windows_amd64-v{0}.zip' -f $env:SPINNER_VERSION) -OutFile spinner.zip -UseBasicParsing ; \
    Expand-Archive spinner.zip ; \
    Remove-Item spinner.zip ; \
    Move-Item spinner\spinner_v$env:SPINNER_VERSION.exe spinner.exe ; \
    Remove-Item spinner

FROM openssh
COPY ./scripts C:/scripts

RUN cd C:/scripts; ./install.ps1
RUN New-Item -Path "C:/ProgramData" -Name "ssh" -ItemType "directory" -Force
RUN New-Item -Path "C:/" -Name "config" -ItemType "directory" -Force
RUN New-Item -Path "C:/" -Name "sftproot" -ItemType "directory" -Force

ENTRYPOINT ["pwsh", "-File", "C:\\scripts\\startup.ps1"]
