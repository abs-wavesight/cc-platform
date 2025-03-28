# powershell image tag; needed for zip extraction.
ARG POWERSHELL_NANOSERVER_TAG=lts-8.2-nanoserver-1809
# nanoserver image tag (contains dotnet).
# Value for windows server 2022: 7.0-nanoserver-ltsc2022
ARG DOTNET_TAG=8.0-nanoserver-1809

# Value for windows server 2022: 7.0-windowsservercore-ltsc2022
ARG ASPNET_FULLSERVER_TAG=8.0-windowsservercore-ltsc2019

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_TAG AS build
ARG ABS_NUGET_USERNAME
ARG ABS_NUGET_PASSWORD
WORKDIR C:/app
COPY ./config ./config
COPY ./services ./services
COPY ./nuget.config ./services

RUN dotnet restore "services/ObservabilityService"
RUN dotnet publish "services/ObservabilityService" --no-restore -c Release -o C:\app\publish /p:UseAppHost=false

# FROM mcr.microsoft.com/dotnet/aspnet:${ASPNET_FULLSERVER_TAG}
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_TAG
WORKDIR C:/app
ENV DOTNET_ENVIRONMENT=docker
COPY --from=build C:/app/publish .
HEALTHCHECK --interval=5s --timeout=5s --start-period=30s --retries=1 CMD curl --fail http://localhost:80/HealthCheck || exit 1
USER containeradministrator
ENTRYPOINT ["dotnet", "Abs.CommonCore.ObservabilityService.dll", "run"]