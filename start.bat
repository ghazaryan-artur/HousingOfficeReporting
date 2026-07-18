@echo off
set "PATH=C:\Program Files (x86)\dotnet;%PATH%"
dotnet run --project "%~dp0src\HousingOffice.App"
pause
