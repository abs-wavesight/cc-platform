# Copied from https://registry.hub.docker.com/r/micdenny/rabbitmq-windows
# ltsc2019 or ltsc2022
ARG SERVER_VERSION='ltsc2019'
ARG POWERSHELL_NANOSERVER_TAG='lts-7.2-nanoserver-1809'

FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION} AS erlang
USER containeradministrator

ENV ERLANG_HOME="c:\\erlang"
ENV ERLANG_VERSION="25.1.1"

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Eownload and install erlang using silent install option, and remove installer when done
RUN Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_25.1.1.exe" -OutFile "c:\\erlang_install.exe" ; \
    Start-Process -Wait -FilePath "c:\\erlang_install.exe" -ArgumentList /S, /D=$env:ERLANG_HOME ; \
    Remove-Item -Force -Path "C:\\erlang_install.exe";


FROM mcr.microsoft.com/powershell:$POWERSHELL_NANOSERVER_TAG AS rabbitmq
USER containeradministrator
LABEL Description="RabbitMQ" Version="3.11.2"

ENV HOMEDRIVE="c:\\"
ENV HOMEPATH="Users\\ContainerAdministrator"
ENV ERLANG_HOME="c:\\erlang"
ENV RABBITMQ_VERSION="3.11.2"
ENV RABBITMQ_CONFIG_FILE="c:\\rabbitmq\\config\\rabbitmq.conf"

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Download and install RabbitMQ
RUN Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v${env:RABBITMQ_VERSION}/rabbitmq-server-windows-$env:RABBITMQ_VERSION.zip" -OutFile "c:\\rabbitmq.zip"
RUN Expand-Archive -Path "c:\\rabbitmq.zip" -DestinationPath "c:\\"
RUN Remove-Item -Force -Path "c:\\rabbitmq.zip"
RUN Rename-Item -Path "c:\\rabbitmq_server-$env:rabbitmq_version" -NewName "c:\\rabbitmq"

# Copy and initialize Erlang
COPY --from=erlang "c:\\erlang" "c:\\erlang"
RUN echo "the-erlang-cookie" > c:\Windows\.erlang.cookie

# Copy handle.exe utility and add to PATH
RUN mkdir "C:\\rabbitmq\\utilities"
COPY ./utilities "C:\\rabbitmq\\utilities"
RUN pwsh -File "C:\\rabbitmq\\utilities\\add-handle-to-path.ps1"

# Create config file
RUN mkdir "C:\\rabbitmq\\config"
RUN ["cmd", "/C", "echo loopback_users = none > c:\\rabbitmq\\config\\rabbitmq.conf"]

# Enable managment plugin
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline

# Run server when container starts - container will shutdown when this process ends
CMD c:\rabbitmq\sbin\rabbitmq-server.bat
