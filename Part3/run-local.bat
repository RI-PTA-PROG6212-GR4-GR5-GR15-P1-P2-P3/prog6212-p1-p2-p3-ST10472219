@echo off
REM Starts the API (port 5080) and the MVC app (port 5000). Run from the Part3 folder, with Part2-API next to it.
start "RaceDay API" cmd /k dotnet run --project ..\Part2-API\src\RaceDay.Api --urls http://localhost:5080
timeout /t 12 >nul
start "RaceDay MVC" cmd /k "set Api__BaseUrl=http://localhost:5080/ && dotnet run --project Part3-MVC --urls http://localhost:5000"
timeout /t 8 >nul
start http://localhost:5000
