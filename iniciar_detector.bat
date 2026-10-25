@echo off
title Game Bottleneck Detector & Diagnostic
cd /d "%~dp0"

echo ===================================================
echo   Iniciando Game Bottleneck Detector & Diagnostic
echo ===================================================
echo.

:: Executa a versão compilada
if exist "bin\Debug\net8.0-windows\GameBottleneckDetector.exe" (
    start "" "bin\Debug\net8.0-windows\GameBottleneckDetector.exe"
) else (
    echo Executando via dotnet run...
    "%LOCALAPPDATA%\dotnet\dotnet.exe" run
)

exit
