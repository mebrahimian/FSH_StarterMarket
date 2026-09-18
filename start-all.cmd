@echo off
title FSH Market Intelligence Launcher

echo Starting API...
start "FSH API" /D "D:\MyAppFSH.MarketIntelligence\src\Host\FSH.Starter.Api" cmd /k "dotnet run --launch-profile https"

echo Waiting 15 seconds for API...
timeout /t 15 /nobreak >nul

echo Starting Dashboard...
start "FSH Dashboard" /D "D:\MyAppFSH.MarketIntelligence\clients\dashboard" cmd /k "npm.cmd run dev -- --port 5174"

echo Waiting 10 seconds for Dashboard...
timeout /t 10 /nobreak >nul

echo Opening Edge...
start "" msedge "http://localhost:5174"

exit