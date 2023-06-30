ARG SERVER_VERSION='ltsc2019'
FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION}
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
# Alternatively, this is how we can download via PowerShell. Caching behvaior seems better with RUN vs ADD.
# RUN Invoke-WebRequest https://github.com/PowerShell/Win32-OpenSSH/releases/download/v9.2.2.0p1-Beta/OpenSSH-Win64-v9.2.2.0.msi -OutFile openssh.msi
# RUN msiexec /i openssh.msi /passive /L openssh-install-output.log
ADD https://github.com/PowerShell/Win32-OpenSSH/releases/download/v9.2.2.0p1-Beta/OpenSSH-Win64.zip OpenSSH-Win64.zip
RUN Expand-Archive -LiteralPath OpenSSH-Win64.zip ; rm OpenSSH-Win64.zip
RUN .\OpenSSH-Win64\OpenSSH-Win64\install-sshd.ps1
ADD ./sshd_config C:/ProgramData/ssh/
ADD ./ssh_host_key C:/ProgramData/ssh/ssh_host_rsa_key
