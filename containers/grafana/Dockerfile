# servercore image tag; need bundled powershell for zip extraction.
# Value for windows server 2022: ltsc2022
ARG SERVER_VERSION='ltsc2019'
# nanoserver image tag (image is much smaller).
# Value for windows server 2022: 7.0-nanoserver-ltsc2022
ARG DOTNET_TAG=7.0-nanoserver-1809

FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION} AS unzip
SHELL [ "powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';" ]
RUN Invoke-WebRequest -UseBasicParsing -Uri "https://dl.grafana.com/oss/release/grafana-9.5.2.windows-amd64.zip" -OutFile grafana.zip ; \
    Expand-Archive -LiteralPath grafana.zip -DestinationPath "C:\\"

FROM mcr.microsoft.com/dotnet/runtime:$DOTNET_TAG AS grafana
COPY --from=unzip --chown=containeruser:containeruser [ "C:/grafana-9.5.2", "C:/Program Files/GrafanaLabs/grafana" ]
# Need to copy over a missing DLL into the nanoserver image
COPY --from=unzip [ "C:/Windows/System32/netapi32.dll", "C:/Windows/System32/netapi32.dll" ]
WORKDIR  C:/Program Files/GrafanaLabs/grafana
EXPOSE 3000
ENTRYPOINT [ "bin\\grafana.exe", "server" ]
