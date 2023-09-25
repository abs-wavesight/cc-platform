# powershell image tag; needed for zip extraction.
ARG POWERSHELL_NANOSERVER_TAG=lts-7.2-nanoserver-1809
# nanoserver image tag (contains dotnet).
# Value for windows server 2022: 7.0-nanoserver-ltsc2022
ARG DOTNET_TAG=7.0-nanoserver-1809

# Value for windows server 2022: 7.0-windowsservercore-ltsc2022
ARG ASPNET_FULLSERVER_TAG=7.0-windowsservercore-ltsc2019

FROM mcr.microsoft.com/powershell:$POWERSHELL_NANOSERVER_TAG AS unzip
SHELL [ "pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';" ]
# Download utils zip to get psping, used for TCP ping health check
WORKDIR C:/app
RUN Invoke-WebRequest -UseBasicParsing -Uri https://download.sysinternals.com/files/PSTools.zip -Outfile PSTools.zip; \
    Expand-Archive -LiteralPath PSTools.zip -DestinationPath .

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_TAG AS build
ARG ABS_NUGET_USERNAME
ARG ABS_NUGET_PASSWORD
WORKDIR C:/app
COPY ./config ./config
COPY ./services ./services
COPY ./nuget.config ./services/SftpService

RUN dotnet restore "services/SftpService"
RUN dotnet publish "services/SftpService" --no-restore -c Release -o C:\app\publish /p:UseAppHost=false

# FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_FULLSERVER_TAG}
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_TAG
WORKDIR C:/app
ENV DOTNET_ENVIRONMENT=docker
COPY --from=unzip --chown=containeruser:containeruser [ "C:/app/psping64.exe", "C:/Windows/psping.exe" ]
COPY --from=build C:/app/publish .
HEALTHCHECK --interval=5s --timeout=5s --start-period=30s --retries=1 CMD [ "psping", "-accepteula", "-q", "-n", "1", "-w", "0", "localhost:1022" ]
USER containeradministrator
ENTRYPOINT ["dotnet", "Abs.CommonCore.Drex.File.Console.dll", "run"]