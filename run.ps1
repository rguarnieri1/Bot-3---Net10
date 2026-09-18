# Bot Cripto - PowerShell Launcher
# Uso: .\run.ps1

$ErrorActionPreference = "Stop"

# Colori per output
function Write-Success {
    Write-Host $args[0] -ForegroundColor Green
}

function Write-Error-Custom {
    Write-Host $args[0] -ForegroundColor Red
}

function Write-Info {
    Write-Host $args[0] -ForegroundColor Cyan
}

# Header
Clear-Host
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║          🤖 BOT CRIPTO - LAUNCHER POWERSHELL          ║" -ForegroundColor Magenta
Write-Host "║                 Versione 1.0                           ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# Verifica .NET SDK
Write-Info "✓ Verificando .NET 10 SDK..."
try {
    $dotnetVersion = dotnet --version
    Write-Success "✅ .NET SDK trovato: $dotnetVersion"
} catch {
    Write-Error-Custom "❌ Errore: .NET SDK non trovato!"
    Write-Host "Scarica .NET 10 da: https://dotnet.microsoft.com/download/dotnet/10.0"
    Read-Host "Premi INVIO per uscire"
    exit 1
}

# Pulizia build precedente (opzionale)
Write-Info "🧹 Pulizia build precedente..."
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
}

# Restore NuGet packages
Write-Info "📦 Ripristino pacchetti NuGet..."
try {
    dotnet restore | Out-Null
    Write-Success "✅ Pacchetti ripristinati"
} catch {
    Write-Error-Custom "❌ Errore nel ripristino pacchetti"
    Read-Host "Premi INVIO per uscire"
    exit 1
}

# Build Release
Write-Info "🔨 Compilazione in Release mode..."
try {
    dotnet build -c Release --no-restore | Out-Null
    Write-Success "✅ Compilazione completata"
} catch {
    Write-Error-Custom "❌ Errore nella compilazione"
    Write-Host $_
    Read-Host "Premi INVIO per uscire"
    exit 1
}

# Esecuzione
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Success "🚀 Avvio Bot Cripto..."
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

Write-Info "Informazioni:"
Write-Info "  • Intervallo: Ogni 5 minuti"
Write-Info "  • Strategie: Bullish Divergence + Zero-Line Crossover"
Write-Info "  • Notifiche: Desktop Enabled"
Write-Info "  • Report Settimanale: Lunedì 00:00"
Write-Info ""
Write-Info "Log e dati salvati in:"
Write-Info "  • Logs/ - File di log"
Write-Info "  • Data/ - Trading record e reports"
Write-Info ""
Write-Info "Premi CTRL+C per fermare il bot"
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host ""

# Avvia il bot
try {
    dotnet run -c Release
} catch {
    Write-Error-Custom "❌ Errore durante l'esecuzione del bot"
    Write-Host $_
    Read-Host "Premi INVIO per uscire"
    exit 1
}
