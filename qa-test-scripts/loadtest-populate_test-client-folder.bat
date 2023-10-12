@echo off
echo +++--- Duplicating Requests for Client: %1 ---+++
cd C:\ABS\TestClient\LoadTest\

echo Copying CC-Disco Config Files
xcopy /f /q /v /y test-client\siemens-config.json %1\
xcopy /f /q /v /y test-client\ConnectionConfig.json %1\
echo F|xcopy /f /q /v /y test-client\test-client-config.json %1\%1-config.json
cd %1
sed -i 's/test-client/%1/g' *.json
cd ..
dir /b test-client\*. > test-client\requestfolders.txt
for /f %%i in (test-client\requestfolders.txt) do loadtest-create_request-folder.bat %1 %%i