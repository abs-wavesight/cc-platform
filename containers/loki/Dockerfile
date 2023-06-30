# Using a powershell image for zip extraction (during build) & HTTP healthcheck (during runtime)
# Value for windows server 2022: lts-7.2-nanoserver-ltsc2022
ARG POWERSHELL_NANOSERVER_TAG=lts-7.2-nanoserver-1809
FROM mcr.microsoft.com/powershell:${POWERSHELL_NANOSERVER_TAG} as unzip
SHELL [ "pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';" ]
WORKDIR C:/app
 RUN Invoke-WebRequest -UseBasicParsing -Uri https://github.com/grafana/loki/releases/download/v2.8.2/loki-windows-amd64.exe.zip -OutFile loki.zip ; \
    Expand-Archive -LiteralPath loki.zip -DestinationPath .

FROM mcr.microsoft.com/powershell:${POWERSHELL_NANOSERVER_TAG} AS loki
COPY --from=unzip --chown=containeruser:containeruser [ "C:/app/loki-windows-amd64.exe", "C:/Program Files/loki/loki.exe" ]
WORKDIR "C:/Program Files/loki"
# Application won't start without configuration file, so adding default config.
# ".yaml" extension is necessary (versus ".yml") for loki to find it
ADD [ "./loki-default-config.yml", "./config.yaml" ]
EXPOSE 3100
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=1 CMD [ "pwsh", "-Command", "Invoke-WebRequest", "-UseBasicParsing", "-Method", "HEAD", "-SkipHttpErrorCheck", "-Uri", "http://localhost:3100" ]
ENTRYPOINT [ "loki.exe" ]

