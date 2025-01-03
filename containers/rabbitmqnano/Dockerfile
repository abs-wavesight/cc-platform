ARG SERVER_VERSION='ltsc2022'

FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION} as unzip

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

FROM mcr.microsoft.com/windows/nanoserver:${SERVER_VERSION}

USER containeradministrator

COPY --from=unzip C:\\erlang C:\\erlang
COPY --from=unzip C:\\rabbitmq C:\\rabbitmq
COPY utilities\\certoc.exe c:\\windows\\system32\\
    COPY utilities\\handle.exe c:\\windows\\system32\\ 

    ENV ERLANG_HOME="c:\\erlang"
ENV RABBITMQ_HOME="c:\\rabbitmq"
ENV HOMEDRIVE="C:"
ENV HOMEPATH="C:"
ENV RABBITMQ_BASE="c:\\data"

# Ports
# 5672: used by AMQP 0-9-1 and 1.0 clients without TLS
# 5671: used by AMQP 0-9-1 and 1.0 clients with TLS
# 15672: HTTP API clients and rabbitmqadmin (only if the management plugin is enabled)
EXPOSE 5672 5671 15671 15672

RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel_management --offline
#
CMD c:\rabbitmq\sbin\rabbitmq-server.bat