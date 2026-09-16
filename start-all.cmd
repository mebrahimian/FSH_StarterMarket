@echo off
title FSH Market Intelligence Launcher

echo Starting API...
start "FSH API" /D "D:\MyAppFSH.MarketIntelligence\src\Host\FSH.Starter.Api" cmd /k "dotnet run"
timeout /t 10 /nobreak >nul

echo Starting Dashboard...
start "FSH Dashboard" /D "D:\MyAppFSH.MarketIntelligence\clients\dashboard" cmd /k "npm run dev -- --port 5173"

echo Waiting...
timeout /t 15 /nobreak >nul

echo Opening Edge...
start "" "microsoft-edge:http://localhost:5173"

exit