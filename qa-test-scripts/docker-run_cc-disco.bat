:: echo off
echo == Starting CC DISCO ==
cd c:\ABS\TestClient\
copy CC-DISCO.log CC-DISCO_backup.log
copy disco-service.log disco-service_backup.log
echo %DATE% %TIME% > disco-service.log
docker run ghcr.io/abs-wavesight/cc-disco:windows-2019 >> disco-service.log 2>&1