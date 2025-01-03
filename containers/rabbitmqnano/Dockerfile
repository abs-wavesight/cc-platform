# escape=`
ARG SERVER_VERSION='ltsc2019'
FROM mcr.microsoft.com/windows/nanoserver:${SERVER_VERSION}

# Copy erlang and rabbit
ADD erlang c:\\erlang
ADD rabbitmq c:\\rabbitmq
ADD erlang_install.exe c:\\
COPY utilities\\certoc.exe c:\\windows\\system32\\
COPY utilities\\handle.exe c:\\windows\\system32\\

RUN c:\\erlang_install.exe /S /D=c:\\erlang

# Environment Variables (ARGs needed to see outer scope ARGs)
ENV ERLANG_HOME="c:\\erlang"
ENV RABBITMQ_HOME="c:\\rabbitmq"
ENV HOMEDRIVE=C:
ENV RABBITMQ_BASE="c:\\data"

# Ports
# 5672: used by AMQP 0-9-1 and 1.0 clients without TLS
# 5671: used by AMQP 0-9-1 and 1.0 clients with TLS
# 15672: HTTP API clients and rabbitmqadmin (only if the management plugin is enabled)
EXPOSE 5672 5671 15671 15672
# USER containeradministrator
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_management --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel --offline
RUN c:\rabbitmq\sbin\rabbitmq-plugins.bat enable rabbitmq_shovel_management --offline

CMD c:\rabbitmq\sbin\rabbitmq-server.bat