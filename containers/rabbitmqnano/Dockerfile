ARG SERVER_VERSION='ltsc2022'

FROM mcr.microsoft.com/windows/servercore:${SERVER_VERSION} as unzip

LABEL Description="RabbitMQ" Version="3.13.7"

# ERLANG_HOME: erlang will install to this location and rabbitmq will use this environment variable to locate it
# RABBITMQ_VERSION: rabbitmq version used in download url and to rename folder extracted from zip file
# RABBITMQ_CONFIG_FILE: tell rabbitmq where to find our custom config file
ENV ERLANG_HOME="c:\\erlang" \
    RABBITMQ_VERSION="3.13.7"

USER ContainerAdministrator
# setup powershell options for RUN commands
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# download and install erlang using silent install option, and remove installer when done
# download and extract rabbitmq, and remove zip file when done
# remove version from rabbitmq folder name
RUN Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_26.2.5.6.exe" -OutFile "c:\\erlang_install.exe"
RUN Start-Process -Wait -FilePath "c:\\erlang_install.exe" -ArgumentList /S, /D=$env:ERLANG_HOME
RUN Remove-Item -Force -Path "C:\\erlang_install.exe"
RUN Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v$env:RABBITMQ_VERSION/rabbitmq-server-windows-$env:RABBITMQ_VERSION.zip" -OutFile "c:\\rabbitmq.zip"
RUN Expand-Archive -Path "c:\\rabbitmq.zip" -DestinationPath "c:\\"
RUN Remove-Item -Force -Path "c:\\rabbitmq.zip"
RUN Rename-Item -Path "c:\\rabbitmq_server-$env:rabbitmq_version" -NewName "c:\\rabbitmq"
RUN Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v3.13.0/rabbitmq_delayed_message_exchange-3.13.0.ez" -OutFile "c:\\rabbitmq\plugins\rabbitmq_delayed_message_exchange-3.13.0.ez"
RUN Invoke-WebRequest -Uri "https://aka.ms/vs/17/release/vc_redist.x64.exe" -OutFile "c:\\VC_redist.x64.exe"
RUN Start-Process -Wait -FilePath "c:\\VC_redist.x64.exe" -ArgumentList /S

# Generate handle.exe utility
RUN Install-PackageProvider -Name NuGet -RequiredVersion 2.8.5.201 -Force
RUN mkdir "C:\\rabbitmq\\utilities"
COPY handle-mimic.ps1 "C:\\rabbitmq\\"
COPY prep-handle-utility.ps1 "C:\\rabbitmq\\"
RUN powershell -File "C:\\rabbitmq\\prep-handle-utility.ps1"
RUN Remove-Item -Force -Path "C:\\rabbitmq\\prep-handle-utility.ps1"
RUN Remove-Item -Force -Path "C:\\rabbitmq\\handle-mimic.ps1"

FROM mcr.microsoft.com/windows/nanoserver:${SERVER_VERSION}

USER ContainerAdministrator

COPY --from=unzip C:\\erlang C:\\erlang
COPY --from=unzip C:\\rabbitmq C:\\rabbitmq
COPY --from=unzip C:\\windows\\system32\\msvcp140.dll C:\\windows\\system32
COPY --from=unzip C:\\windows\\system32\\vcruntime140.dll C:\\windows\\system32
COPY --from=unzip C:\\windows\\system32\\vcruntime140_1.dll C:\\windows\\system32
COPY --from=unzip C:\\windows\\system32\\certoc.exe C:\\windows\\system32
COPY --from=unzip C:\\rabbitmq\\utilities\\handle.exe c:\\windows\\system32
RUN mkdir "C:\\rabbitmq\\config"
RUN ["cmd", "/C", "echo loopback_users = none > c:\\rabbitmq\\config\\rabbitmq.conf"]

ENV ERLANG_HOME=C:/erlang 
ENV RABBITMQ_BASE=C:/rabbitmq/data 
ENV RABBITMQ_CONFIG_FILE=C:/rabbitmq/config/rabbitmq.conf
ENV HOMEDRIVE="C:\\"
ENV HOMEPATH=Users\\ContainerAdministrator

RUN setx /M PATH "%PATH%;%ERLANG_HOME%\bin"
# Ports
# 5672: used by AMQP 0-9-1 and 1.0 clients without TLS
# 5671: used by AMQP 0-9-1 and 1.0 clients with TLS
# 15672: HTTP API clients and rabbitmqadmin (only if the management plugin is enabled)
EXPOSE 5672 5671 15671 15672

RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel_management --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_auth_mechanism_ssl --offline
#
CMD c:\rabbitmq\sbin\rabbitmq-server.bat