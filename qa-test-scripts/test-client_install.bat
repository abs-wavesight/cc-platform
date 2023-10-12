echo Installing DISCO Test Client
cd C:\ABS\TestClient\
setVariables_ABS-Nuget.bat LNevarezTalavera
dotnet tool install Abs.CommonCore.Disco.TestClient --global --configfile nuget.config