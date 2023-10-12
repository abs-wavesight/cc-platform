@echo off
set LogFolder=%1
echo Looking for Warning or Error log entries in the CC-DISCO.log and Siemens-Adapter.log files
echo +++ Errors Report for: %2 +++ 1>  %LogFolder%\Warnings-Errors.log
echo Start Date-Time: %StartDateTime%  >> %LogFolder%\Warnings-Errors.log
echo ----------------------------------------------------------------------- >> %LogFolder%\Warnings-Errors.log 
echo. >> %LogFolder%\Warnings-Errors.log
echo +++ TestRun_%2.log +++ 1>>  %LogFolder%\Warnings-Errors.log
grep -E "Warning|Error|BAD_REQUEST" %LogFolder%\TestRun_%2.log    | grep -v "CleanupTrackedErrors" >> %LogFolder%\Warnings-Errors.log
echo +++ disco-service +++ 1>>  %LogFolder%\Warnings-Errors.log
grep -E "Warning|Error|fail:|BAD_REQUEST" %LogFolder%\disco-service_%2.log   | grep -v "CleanupTrackedErrors" >> %LogFolder%\Warnings-Errors.log
echo +++ siemens-adapter +++ 1>>  %LogFolder%\Warnings-Errors.log
grep -E "Warning|Error|fail:|BAD_REQUEST" %LogFolder%\siemens-adapter_%2.log | grep -v "CleanupTrackedErrors" >> %LogFolder%\Warnings-Errors.log
echo. >> %LogFolder%\Warnings-Errors.log
echo ----------------------------------------------------------------------- >> %LogFolder%\Warnings-Errors.log
echo End Date-Time: %EndDateTime% >> %LogFolder%\Warnings-Errors.log
echo Errors logged into %LogFolder%\Warnings-Errors.log