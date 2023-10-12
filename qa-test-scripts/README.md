# qa-test-scrips
The Scripts in this folder will facilitate the Installation, Configuration and Testing (functional / load) of CC-Disco and CC-Adapters-Siemens projects

# Test Script Files

README.md                   This File

test-client_execute.bat     executes the test-client, copies log files
test-client_install.bat     installs the dotnet nuget tool
test-client_update.bat      updates the dotnet nuget tool
dev-utility_install.bat     installs the dotnet nuget tool
dev-utility_update.bat      updates the dotnet nuget tool
set-Variables_ABS-Nuget.bat set variables used by the install / update batchs ^^

loadtest-create_test-clients-folders.bat    create test-client-x/request-type folders to be used by the LoadTest
loadtest-populate_test-client-folder.bat    called by batch above
loadtest-create_request-folder.bat          called by batch above
loadtest-create_test-clients-queues.bat     called by batch above

loadtest-start.bat                  starts LoadTest for [arg-1-qty] test-clients for a [arg-2-requests-folder] 
loadtest-execute_test-client.bat    called by batch above

logs_check-errors.bat       extract errors from cc-disco, siemens-adapter and TestRun logs, created Warning-Errors.log file
logs_get-summary.bat        called by loadtest-start, creates a Summary report for each test-client-x

logs_compare.bat            compares 2 Log files, replace-timestamps should be called first
logs_replace-timestamps.bat replaces the time stamps with 'YYYY-MM-DD HH:MM:SS.sss" mask, so the compare will be cleaner

copy_cc-disco-configs.bat   copies the [arg1-test-client]-config and siemens-config into the cc-disco container 
cd_cc-disco.bat
cd_test-client.bat
cd_test-scripts.bat
get_date-time.bat
notes.txt

docker-login.bat

docker-pull_adapters-siemens.bat        not longer needed, functionality replaced by cc-local dev-utility
docker-pull_cc-disco.bat                not longer needed, functionality replaced by cc-local dev-utility
docker-pull_rabbit-mq.bat               not longer needed, functionality replaced by cc-local dev-utility
docker-rm_all-images.bat                not longer needed, functionality replaced by cc-local dev-utility
docker-run_all-siemens-adapter.bat      not longer needed, functionality replaced by cc-local dev-utility
docker-run_cc-disco.bat                 not longer needed, functionality replaced by cc-local dev-utility
docker-run_rabbit-mq.bat                not longer needed, functionality replaced by cc-local dev-utility
docker-run_siemens-adapter.bat          not longer needed, functionality replaced by cc-local dev-utility