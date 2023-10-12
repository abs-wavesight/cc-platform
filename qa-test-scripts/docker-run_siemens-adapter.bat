echo == Starting Siemens Adapter ==
cd c:\ABS\TestClient\
copy Siemens-Adapter.log siemens-adapter_backup1.log
copy siemens-adapter.log siemens-adapter_backup.log
echo %DATE% %TIME% > siemens-adapter.log
docker run ghcr.io/abs-wavesight/cc-adapters-siemens:windows-2019 >> siemens-adapter.log 2>&1