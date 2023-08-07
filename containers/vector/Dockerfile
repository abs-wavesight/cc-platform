# servercore image tag; need bundled powershell for zip extraction.
# Value for windows server 2022: ltsc2022
ARG SERVER_VERSION='ltsc2019'

ARG POWERSHELL_NANOSERVER_TAG='lts-7.2-nanoserver-1809'

# nanoserver image tag (image is much smaller).
# Value for windows server 2022: 7.0-nanoserver-ltsc2022
ARG DOTNET_TAG=7.0-nanoserver-1809

FROM mcr.microsoft.com/powershell:$POWERSHELL_NANOSERVER_TAG AS vector
# Running with elevated privileges to read from the docker daemon named pipe
USER containeradministrator

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
# Preferring `RUN Invoke-WebRequest` vs `ADD` due to preferable caching behavior.
RUN Invoke-WebRequest -UseBasicParsing -Uri https://packages.timber.io/vector/0.31.0/vector-0.31.0-x86_64-pc-windows-msvc.zip -OutFile vector.zip ; \
    Expand-Archive -LiteralPath vector.zip -DestinationPath "$Env:ProgramFiles\\Vector"
RUN New-Item -Path 'C:\' -Name 'config' -ItemType Directory -Force
RUN New-Item -Path 'C:\' -Name 'cc-logs' -ItemType Directory -Force
COPY ./scripts C:/scripts

ENTRYPOINT ["pwsh", "-File", "C:\\scripts\\startup.ps1"]