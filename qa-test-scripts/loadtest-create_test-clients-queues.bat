echo off
echo +++--- Creating Test Clients queues: %1 ---+++
cd C:\ABS\TestClient\LoadTest\

echo Copying CC-Disco Configs (Creating Exchanges, Queues)
for /l %%i in (1,1,%1) do copy_cc-disco-configs.bat test-client-%%i