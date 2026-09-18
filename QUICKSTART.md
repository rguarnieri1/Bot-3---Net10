# ⚡ Quick Start - Bot Cripto

Inizia in meno di 2 minuti!

## 1️⃣ Prerequisiti

- ✅ .NET 10 SDK ([scarica qui](https://dotnet.microsoft.com/download/dotnet/10.0))
- ✅ Windows 10+ (per le notifiche desktop)
- ✅ Connessione Internet

Verifica l'installazione:
```bash
dotnet --version
```

## 2️⃣ Avvio Rapido

### Opzione A: Script Batch (Consigliato per Windows)
Doppio click su `run.bat` e il bot partirà automaticamente!

### Opzione B: Terminale
```bash
cd "C:\Users\Pc\OneDrive\Bot\1 - Bot Cripto"
dotnet run
```

### Opzione C: PowerShell
```powershell
cd "C:\Users\Pc\OneDrive\Bot\1 - Bot Cripto"
dotnet run
```

## 3️⃣ Output Atteso

Vedrai:
```
╔════════════════════════════════════════════════════════╗
║          🤖 BOT CRIPTO - TRADING AUTOMATICODE          ║
║                 Versione 1.0                           ║
╚════════════════════════════════════════════════════════╝

Strategie Attive:
  • Bullish Divergence
  • Zero-Line Crossover MACD

Impostazioni:
  • Intervallo Monitoraggio: 5 minuti
  • Report Settimanale: Lunedì 00:00
  • Notifiche Desktop: Abilitate

⏰ Controllo mercato: 2026-08-31 14:30:45
📈 Analizzando 50 criptovalute...
✅ Controllo completato. Segnali trovati: 2
```

## 4️⃣ Come Leggere i Segnali

Quando il bot trova un segnale:

**🚀 ACQUISTO (Bullish)**
```
🚀 SEGNALE: BUY - Bullish Divergence Detected
   Crypto: BTC
   Prezzo: $42500.50
   Strategia: Bullish Divergence
```

**⛔ VENDITA (Bearish)**
```
⛔ SEGNALE: SELL - MACD Bearish Zero-Line Crossover
   Crypto: ETH
   Prezzo: $2300.75
   Strategia: Zero-Line Crossover
```

## 5️⃣ File Generati

Dopo il primo run, il bot crea automaticamente:

```
1 - Bot Cripto/
├── Data/
│   ├── trades.json               ← Tutte le operazioni
│   └── Reports/
│       └── Weekly_Report_*.txt    ← Report settimanali
└── Logs/
    └── signals_2026-08-31.log     ← Log dei segnali
```

## 6️⃣ Leggere il Weekly Report

Ogni lunedì il bot genera un report tipo:

```
═══════════════════════════════════════════════════════════
📈 REPORT SETTIMANALE - 2026-08-25 a 2026-09-01

📊 STATISTICHE GENERALI:
   • Operazioni Totali: 12
   • Operazioni Chiuse: 8
   • Operazioni Aperte: 4

💰 PERFORMANCE:
   • Profitto Totale: $245.50
   • ROI Medio: 2.15%
   • Win Rate: 75.00%

🎯 OPERAZIONI CHIUSE:
   ✅ BTC: Entry $42500.00 → Exit $43000.00 (1.17%) | Bullish Divergence
   ❌ ETH: Entry $2300.00 → Exit $2250.00 (-2.17%) | Zero-Line Crossover
```

## 7️⃣ Personalizzazione Semplice

### Modificare l'Intervallo di Controllo

Apri `Services/BotSchedulerService.cs` e cambia:

```csharp
// Cambiar da 5 a 10 minuti
TimeSpan.FromMinutes(5)  ← Cambia il numero
```

### Abilitare/Disabilitare Strategie

Apri `Services/BotSchedulerService.cs` e commenta la strategia:

```csharp
// Disabilita Bullish Divergence
// var bullishResult = _bullishStrategy.Analyze(crypto.Symbol, candles);

// Disabilita Zero-Line Crossover
var crossoverResult = _crossoverStrategy.Analyze(crypto.Symbol, candles);
```

### Limite di Cripto da Analizzare

```csharp
// Analyza solo le prime 20 cripto (default: 50)
foreach (var crypto in cryptos.Take(20))
```

## 8️⃣ Troubleshooting

### "dotnet: command not found"
→ Installa .NET 10 SDK da https://dotnet.microsoft.com/download/dotnet/10.0

### Il bot non manda notifiche
→ Verifica che Windows Notifications sia abilitato
→ Controlla il file di log in `Logs/signals_*.log`

### Errore di connessione a Crypto.com
→ Verifica la connessione internet
→ L'API di Crypto.com potrebbe essere temporaneamente down

### Alto utilizzo CPU
→ Aumenta l'intervallo di monitoraggio (es: 10 minuti)
→ Riduci il numero di cripto da analizzare (`.Take(20)`)

## 9️⃣ Prossimi Passi

1. **Leggere il README**: Comprendi meglio come funziona
2. **Aggiungere Strategie**: Vedi [EXTENDING.md](EXTENDING.md)
3. **Customizzare**: Modifica i parametri in `config.json`
4. **Monitor**: Tieni traccia dei report settimanali

## 🔟 Risorse

- 📖 [README Completo](README.md)
- 🔧 [Guida Estensione](EXTENDING.md)
- 📊 [Config.json Spiegato](#configurazione)

---

**Buon trading! 🚀**

*Il bot è in beta - segnala bug a: roberto.guarnieri2@gmail.com*
