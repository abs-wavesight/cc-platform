echo off
@echo Updating DISCO Test Client
cd c:\ABS\TestClient\
echo dotnet tool update Abs.CommonCore.Disco.TestClient --global --configfile c:\ABS\TestClient\nuget.config
set-Variables_ABS-Nuget.bat LNevarezTalavera
dotnet tool update Abs.CommonCore.Disco.TestClient --global --configfile c:\ABS\TestClient\nuget.config