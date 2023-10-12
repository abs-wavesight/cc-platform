@echo off
echo Creating Request folder: %2 for test client: %1
mkdir %1\%2
xcopy /q /v /y test-client\%2\*.json C:\ABS\TestClient\LoadTest\%1\%2\
echo replacing "test-client" for "%1" in %2 Json Requests
cd %1\%2\
for %%i in (%2*.json) do ren %%i %1_%%i
sed -i 's/test-client/%1/g' *.json
cd C:\ABS\TestClient\LoadTest\