# Copied from https://registry.hub.docker.com/r/micdenny/rabbitmq-windows
# ltsc2019 or ltsc2022
ARG SERVER_VERSION='ltsc2019'
FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION}

LABEL Description="RabbitMQ" Version="3.8.15"

# ERLANG_HOME: erlang will install to this location and rabbitmq will use this environment variable to locate it
# RABBITMQ_VERSION: rabbitmq version used in download url and to rename folder extracted from zip file
# RABBITMQ_CONFIG_FILE: tell rabbitmq where to find our custom config file
ENV ERLANG_HOME="c:\\erlang" \
    RABBITMQ_VERSION="3.8.15" \
    RABBITMQ_CONFIG_FILE="c:\\rabbitmq\\config\\rabbitmq.conf"

# setup powershell options for RUN commands
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# download and install erlang using silent install option, and remove installer when done
# download and extract rabbitmq, and remove zip file when done
# remove version from rabbitmq folder name
RUN Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_22.3.exe" -OutFile "c:\\erlang_install.exe" ; \
    Start-Process -Wait -FilePath "c:\\erlang_install.exe" -ArgumentList /S, /D=$env:ERLANG_HOME ; \
    Remove-Item -Force -Path "C:\\erlang_install.exe" ; \
    Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v$env:RABBITMQ_VERSION/rabbitmq-server-windows-$env:RABBITMQ_VERSION.zip" -OutFile "c:\\rabbitmq.zip" ; \
    Expand-Archive -Path "c:\\rabbitmq.zip" -DestinationPath "c:\\" ; \
    Remove-Item -Force -Path "c:\\rabbitmq.zip" ; \
    Rename-Item -Path "c:\\rabbitmq_server-$env:rabbitmq_version" -NewName "c:\\rabbitmq" ; \
    Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/3.8.9/rabbitmq_delayed_message_exchange-3.8.9-0199d11c.ez" -OutFile "c:\\rabbitmq\plugins\rabbitmq_delayed_message_exchange-3.8.9.ez"

# Copy handle.exe utility and add to PATH
RUN mkdir "C:\\rabbitmq\\utilities"
COPY ./utilities "C:\\rabbitmq\\utilities"
RUN powershell -File "C:\\rabbitmq\\utilities\\add-handle-to-path.ps1"

# create config file
RUN mkdir "C:\\rabbitmq\\config"
RUN ["cmd", "/C", "echo loopback_users = none > c:\\rabbitmq\\config\\rabbitmq.conf"]

# enable managment plugin
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel_management --offline

# run server when container starts - container will shutdown when this process ends
CMD c:\rabbitmq\sbin\rabbitmq-server.bat
