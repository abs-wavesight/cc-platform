# Copied from https://registry.hub.docker.com/r/micdenny/rabbitmq-windows
# ltsc2019 or ltsc2022
ARG SERVER_VERSION='ltsc2019'
FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION}

LABEL Description="RabbitMQ" Version="3.11.2"

# ERLANG_HOME: erlang will install to this location and rabbitmq will use this environment variable to locate it
# RABBITMQ_VERSION: rabbitmq version used in download url and to rename folder extracted from zip file
# RABBITMQ_CONFIG_FILE: tell rabbitmq where to find our custom config file
ENV ERLANG_HOME="c:\\erlang" \
    RABBITMQ_VERSION="3.11.2" \
    RABBITMQ_CONFIG_FILE="c:\\rabbitmq\\config\\rabbitmq.conf"

USER ContainerAdministrator
# setup powershell options for RUN commands
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# download and install erlang using silent install option, and remove installer when done
# download and extract rabbitmq, and remove zip file when done
# remove version from rabbitmq folder name
RUN Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_25.1.1.exe" -OutFile "c:\\erlang_install.exe" ; \
    Start-Process -Wait -FilePath "c:\\erlang_install.exe" -ArgumentList /S, /D=$env:ERLANG_HOME ; \
    Remove-Item -Force -Path "C:\\erlang_install.exe" ; \
    Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v$env:RABBITMQ_VERSION/rabbitmq-server-windows-$env:RABBITMQ_VERSION.zip" -OutFile "c:\\rabbitmq.zip" ; \
    Expand-Archive -Path "c:\\rabbitmq.zip" -DestinationPath "c:\\" ; \
    Remove-Item -Force -Path "c:\\rabbitmq.zip" ; \
    Rename-Item -Path "c:\\rabbitmq_server-$env:rabbitmq_version" -NewName "c:\\rabbitmq"

# Generate handle.exe utility
RUN Install-PackageProvider -Name NuGet -RequiredVersion 2.8.5.201 -Force
RUN mkdir "C:\\rabbitmq\\utilities"
COPY handle-mimic.ps1 "C:\\rabbitmq"
COPY prep-handle-utility.ps1 "C:\\rabbitmq"
RUN powershell -File "C:\\rabbitmq\\prep-handle-utility.ps1"
RUN Remove-Item -Force -Path "C:\\rabbitmq\\prep-handle-utility.ps1"
RUN Remove-Item -Force -Path "C:\\rabbitmq\\handle-mimic.ps1"

# Copy handle.exe utility and add to PATH
COPY ./utilities "C:\\rabbitmq\\utilities"
RUN powershell -File "C:\\rabbitmq\\utilities\\add-handle-to-path.ps1"

# create config file
RUN mkdir "C:\\rabbitmq\\config"
RUN ["cmd", "/C", "echo loopback_users = none > c:\\rabbitmq\\config\\rabbitmq.conf"]

# enable managment plugin
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline

# run server when container starts - container will shutdown when this process ends
CMD c:\rabbitmq\sbin\rabbitmq-server.bat
