cd ".\containers\rabbitmqnano"
Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_22.3.exe" -OutFile ".\erlang_install.exe"
Start-Process -Wait erlang_install.exe -ArgumentList "/S /D=D:\a\cc-platform\cc-platform\containers\rabbitmqnano\erlang\"
dir
#Remove-Item -Force -Path ".\erlang_install.exe"
Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v3.8.15/rabbitmq-server-windows-3.8.15.zip" -OutFile ".\rabbitmq.zip"
Expand-Archive -Path ".\rabbitmq.zip" -DestinationPath ".\"
Remove-Item -Force -Path ".\rabbitmq.zip"
dir
Rename-Item -Path ".\rabbitmq_server-3.8.15" -NewName ".\rabbitmq"
Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/3.8.9/rabbitmq_delayed_message_exchange-3.8.9-0199d11c.ez" -OutFile ".\rabbitmq\plugins\rabbitmq_delayed_message_exchange-3.8.9.ez"
cd ".\erlang"
dir
cd ".\erts-10.7\bin"
dir
