@echo off
echo == Copying CC-Disco Config files ==
docker ps | grep 'cc-disco' | awk -F ' '  '{print $1}' 1>cc-disco-containerId.txt
timeout 3 1>NUL
set /p containerId=<cc-disco-containerId.txt

echo Copying configs for client folder: %1 into container: %containerId%
echo %TIME%
echo %1\%1-config.json
docker cp %1\%1-config.json %containerId%:/app/config/client-configs
timeout /t 3 1>NUL
echo %TIME%
echo %1\siemens-config.json
docker cp %1\siemens-config.json %containerId%:/app/config/vendor-configs
echo %TIME%
echo on