@echo off
title Game Bottleneck Detector & Diagnostic v1.1.0
cd /d "%~dp0"

echo ===================================================
echo   Iniciando Game Bottleneck Detector v1.1.0
echo ===================================================
echo.

if exist "publish_v1.1\GameBottleneckDetector.exe" (
    start "" "publish_v1.1\GameBottleneckDetector.exe"
) else if exist "publish\GameBottleneckDetector.exe" (
    start "" "publish\GameBottleneckDetector.exe"
) else (
    echo Executando via dotnet run...
    "%LOCALAPPDATA%\dotnet\dotnet.exe" run
)

exit
