Invoke-WebRequest -Uri "https://erlang.org/download/otp_win64_25.1.1.exe" -OutFile ".\erlang_install.exe"
Start-Process -Wait -FilePath ".\erlang_install.exe" -ArgumentList /S, /D=".\containers\rabbitmqnano\erlang\"
#Remove-Item -Force -Path ".\erlang_install.exe"
Invoke-WebRequest -Uri "https://github.com/rabbitmq/rabbitmq-server/releases/download/v3.8.15/rabbitmq-server-windows-3.8.15.zip" -OutFile ".\rabbitmq.zip"
Expand-Archive -Path ".\rabbitmq.zip" -DestinationPath ".\containers\rabbitmqnano"
Remove-Item -Force -Path ".\rabbitmq.zip"
Rename-Item -Path ".\containers\rabbitmqnano\rabbitmq_server-3.8.15" -NewName ".\containers\rabbitmqnano\rabbitmq"
cd ".\containers\rabbitmqnano"
dir