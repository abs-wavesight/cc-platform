@echo off
cd C:\ABS\TestClient\TestClientRequests\LoadTest\

echo ++--- Executing LoadTest: %1 for test client: %2 with test cases from folder TestClientRequests\%1\%2 --++
get_date-time.bat | awk -F ':' '{print $1}' > _temp_DateHour.txt
set /p DateHour=<_temp_DateHour.txt
echo saving logs to LogFolder\LoadTest-%DateHour%_%1\%2
mkdir c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2
set StartDateTime=%DATE% %TIME%
echo Start Date-Time: %StartDateTime%
echo Start Date-Time: %StartDateTime% > c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2\start-end_date-time.log
echo on
disco-test-client C:\ABS\TestClient\LoadTest\%2\%1 C:\ABS\TestClient\LoadTest\%2\ConnectionConfig.json C:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2
echo off
set EndDateTime=%DATE% %TIME%
echo End Date-Time: %EndDateTime%
echo Copying CC-Disco and Siemens Adapter log files to LogFolder\LoadTest-%DateHour%_%1\%2
echo F|xcopy /f /q /v /y C:\ABS\cc-platform\logs\site\disco-service-%DateHour%.log   c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2\disco-service_LoadTest-%DateHour%_%1_%2.log
echo F|xcopy /f /q /v /y C:\ABS\cc-platform\logs\site\siemens-adapter-%DateHour%.log c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2\siemens-adapter_LoadTest-%DateHour%_%1_%2.log
copy /v c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2\TestRun.log c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2\TestRun_LoadTest-%DateHour%_%1_%2.log
logs_check-errors.bat c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\%2 LoadTest-%DateHour%_%1_%2
