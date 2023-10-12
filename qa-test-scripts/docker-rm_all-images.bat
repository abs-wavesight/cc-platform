@echo off
cd c:\ABS\TestClient\

echo ++++ Killing Docker Containers ++++
for /f %%i in ('docker ps -q') do docker kill %%i

echo ++++ Current Docker Images ++++
docker image ls
echo.
echo ++++ Removing all CC Docker Images ++++
docker rmi --force ghcr.io/abs-wavesight/cc-adapters-siemens:windows-2019
docker rmi --force ghcr.io/abs-wavesight/cc-disco:windows-2019
docker rmi --force ghcr.io/abs-wavesight/vector:windows-2019
:: docker rmi --force ghcr.io/abs-wavesight/rabbitmq:windows-2019
echo.
echo ++++ ABS CC Disco, Siemens-Adapter and Vector Images Removed ... Images that still exists are: ++++
docker image ls