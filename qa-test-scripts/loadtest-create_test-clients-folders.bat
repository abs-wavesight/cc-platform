echo off
echo +++--- Creating Test Clients qty: %1 ---+++
cd C:\ABS\TestClient\LoadTest\

echo Creating test-client-xx folders, qty: %1\
for /l %%i in (1,1,%1) do mkdir test-client-%%i

echo Populating requests
for /l %%i in (1,1,%1) do loadtest-populate_test-client-folder.bat test-client-%%i

echo Copying CC-Disco Configs (Creating Exchanges, Queues)
for /l %%i in (1,1,%1) do copy_cc-disco-configs.bat test-client-%%i