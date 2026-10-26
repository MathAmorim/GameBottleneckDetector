@echo off
title Game Bottleneck Detector & Diagnostic v1.2.0
cd /d "%~dp0"

echo ===================================================
echo   Iniciando Game Bottleneck Detector v1.2.0
echo ===================================================
echo.

if exist "GameBottleneckDetector.exe" (
    start "" "GameBottleneckDetector.exe"
) else if exist "publish_standalone\GameBottleneckDetector.exe" (
    start "" "publish_standalone\GameBottleneckDetector.exe"
) else if exist "publish_v1.2\GameBottleneckDetector.exe" (
    start "" "publish_v1.2\GameBottleneckDetector.exe"
) else if exist "publish\GameBottleneckDetector.exe" (
    start "" "publish\GameBottleneckDetector.exe"
) else if exist "publish_v1.1\GameBottleneckDetector.exe" (
    start "" "publish_v1.1\GameBottleneckDetector.exe"
) else (
    echo Executando via dotnet run...
    "%LOCALAPPDATA%\dotnet\dotnet.exe" run
)

exit
