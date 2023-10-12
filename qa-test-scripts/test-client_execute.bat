@echo off
cd c:\ABS\TestClient\
get_date-time.bat | awk -F ':' '{print $1}' > _temp_DateHour.txt
set /p DateHour=<_temp_DateHour.txt
echo ++--- Executing test client, test cases from folder TestClientRequests\%1 --++
echo saving logs to LogFolder\%1-%2_%DateHour%
set StartDateTime=%DATE% %TIME%
echo Start Date-Time: %StartDateTime%
:: echo on
:: echo Start Date-Time: %DateTime% > c:\ABS\TestClient\LogFolder-%1-%2_%DateHour%\start-end_date-time.log
disco-test-client TestClientRequests\%1 C:\ABS\TestClient\ConnectionConfig.json c:\ABS\TestClient\LogFolder\%1-%2_%DateHour%
set EndDateTime=%DATE% %TIME%
echo off
echo Copying CC-Disco and Siemens Adapter log files to LogFolder\%1-%2_%DateHour%
cd c:\ABS\TestClient\LogFolder\%1-%2_%DateHour%\
:: copy c:\ABS\TestClient\disco-service.log
:: copy c:\ABS\TestClient\siemens-adapter.log
copy /v C:\ABS\cc-platform\logs\site\disco-service-%DateHour%.log   C:\ABS\TestClient\LogFolder\%1-%2_%DateHour%\disco-service_%1-%2_%DateHour%.log
copy /v C:\ABS\cc-platform\logs\site\siemens-adapter-%DateHour%.log C:\ABS\TestClient\LogFolder\%1-%2_%DateHour%\siemens-adapter_%1-%2_%DateHour%.log
copy /v TestRun.log TestRun_%1-%2_%DateHour%.log
logs_check-errors.bat C:\ABS\TestClient\LogFolder\%1-%2_%DateHour% %1-%2_%DateHour%
logs_get-summary.bat C:\ABS\TestClient\LogFolder\%1-%2_%DateHour% %1-%2_%DateHour%