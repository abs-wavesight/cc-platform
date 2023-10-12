echo off
echo +++--- Starting CC-DISCO / Siemens Adapter Load Test ---+++
echo --- This script requires 2 arguments: Request type OnDemand/Schedule: %1 and TestClients qty: %2
set StartDateTime=%DATE% %TIME%
echo Start Date-Time: %StartDateTime%
cd C:\ABS\TestClient\LoadTest\
echo --- Creating %2 Test Client instances in independent Command Prompt Windows ---
for /l %%i in (1,1,%2) do timeout /t 1 | start "test-client-%%i" loadtest-execute_test-client %1 test-client-%%i
set /p DateHour=<_temp_DateHour.txt
echo Pausing for TestClients to Complete ...
pause
set EndDateTime=%DATE% %TIME%
echo End Date-Time: %EndDateTime%
echo about to Get Summaries for LoadTest: %DateHour% %1
for /l %%i in (1,1,%2) do logs_get-summary.bat c:\ABS\TestClient\LogFolder\LoadTest-%DateHour%_%1\test-client-%%i LoadTest-%DateHour%_%1_test-client-%%i
