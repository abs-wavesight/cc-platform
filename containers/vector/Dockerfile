# servercore image tag; need bundled powershell for zip extraction.
# Value for windows server 2022: ltsc2022
ARG SERVER_VERSION='ltsc2019'
# nanoserver image tag (image is much smaller).
# Value for windows server 2022: 7.0-nanoserver-ltsc2022
ARG DOTNET_TAG=7.0-nanoserver-1809

FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION} AS unzip
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
# Preferring `RUN Invoke-WebRequest` vs `ADD` due to preferable caching behavior.
RUN Invoke-WebRequest -UseBasicParsing -Uri https://packages.timber.io/vector/latest/vector-latest-x86_64-pc-windows-msvc.zip -OutFile vector.zip ; \
    Expand-Archive -LiteralPath vector.zip -DestinationPath "$Env:ProgramFiles\\Vector\\"

FROM mcr.microsoft.com/dotnet/runtime:$DOTNET_TAG AS vector
# Running with elevated privileges to read from the docker daemon named pipe
USER containeradministrator
COPY --from=unzip ["C:/Program Files/Vector/", "C:/Program Files/Vector/"]
RUN ["cmd", "/C", "mkdir", "C:\\config"]
ENTRYPOINT ["C:\\Program Files\\Vector\\bin\\vector.exe"]
