@echo off
chcp 65001 > nul
echo.
echo ╔════════════════════════════════════════════════════════╗
echo ║          🤖 BOT CRIPTO - AVVIO                         ║
echo ╚════════════════════════════════════════════════════════╝
echo.
echo Compilazione in corso...
dotnet build -c Release > nul 2>&1

if errorlevel 1 (
    echo ❌ Errore nella compilazione!
    pause
    exit /b 1
)

echo ✅ Compilazione completata!
echo.
echo 🚀 Avvio bot in corso...
echo.
dotnet run -c Release

pause
