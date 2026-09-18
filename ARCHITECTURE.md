# 🏗️ Architecture - Bot Cripto

Documentazione architetturale e flussi dati del bot.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    BOT CRIPTO v1.0                          │
└─────────────────────────────────────────────────────────────┘

                         ┌──────────────────┐
                         │  Program.Main()  │
                         │ (Entry Point)    │
                         └────────┬─────────┘
                                  │
                         ┌────────▼─────────┐
                         │ BotScheduler     │
                         │ Service          │
                         │                  │
                         │ • Timer 5 min    │
                         │ • Timer weekly   │
                         └────────┬─────────┘
                                  │
                ┌─────────────────┼─────────────────┐
                │                 │                 │
        ┌───────▼────────┐  ┌─────▼──────┐  ┌──────▼─────────┐
        │   CryptoData   │  │ Strategy   │  │ Notification  │
        │   Service      │  │ Manager    │  │ Service       │
        │                │  │            │  │               │
        │ • Get ticker   │  │ • Analyze  │  │ • Desktop     │
        │ • Get candles  │  │   signals  │  │ • Logging     │
        └───────┬────────┘  └─────┬──────┘  └──────┬─────────┘
                │                 │                 │
     ┌──────────▼─────┐  ┌────────▼───────────┐   │
     │ Crypto.com API │  │ TechnicalIndicators│   │
     │                │  │                    │   │
     │ • Tickers      │  │ • RSI (14)         │   │
     │ • OHLCV        │  │ • MACD (12/26/9)   │   │
     │ • Orderbook    │  │ • EMA/SMA          │   │
     └──────────────────┘ │ • BB (20/2)       │   │
                          └──────┬────────────┘   │
                                 │                │
                   ┌─────────────┬┴────────────────┴─────────┐
                   │             │                          │
        ┌──────────▼────────┐ ┌──▼──────────────┐ ┌────────▼───┐
        │ BullishDivergence │ │ MACD Crossover  │ │ Reporting  │
        │ Strategy          │ │ Strategy        │ │ Service    │
        │                   │ │                 │ │            │
        │ Signals: BUY      │ │ Signals: BUY/   │ │ • Save     │
        │                   │ │ SELL            │ │   trades   │
        └───────────────────┘ └─────────────────┘ │ • Stats    │
                                                  │ • Report   │
                                                  └────────────┘

                         ┌──────────────────┐
                         │  File Storage    │
                         │                  │
                         │ • trades.json    │
                         │ • signals_*.log  │
                         │ • Weekly_*.txt   │
                         └──────────────────┘
```

---

## Data Flow Diagram

### Market Analysis Cycle (Every 5 minutes)

```
┌─────────────────────────────────────┐
│ Timer Tick (5 minutes)               │
└────────────────────┬────────────────┘
                     │
    ┌────────────────▼────────────────┐
    │ Get Cryptocurrencies             │
    │ (CryptoDataService.GetTicker)    │
    │ Returns: List<Cryptocurrency>    │
    └────────────────┬────────────────┘
                     │
    ┌────────────────▼────────────────┐
    │ For Each Crypto (Top 50)        │
    │                                  │
    │ ┌──────────────────────────┐    │
    │ │ Get Historical Candles    │    │
    │ │ (GetCandlesAsync "4h",100)│    │
    │ │ Returns: List<Candle>     │    │
    │ └──────────┬───────────────┘    │
    │            │                    │
    │ ┌──────────▼───────────────┐    │
    │ │ Analyze with Strategies   │    │
    │ │                           │    │
    │ │ 1. BullishDivergence.     │    │
    │ │    Analyze()              │    │
    │ │    ├─ Calc RSI            │    │
    │ │    ├─ Find divergences    │    │
    │ │    └─ Return signal (Y/N) │    │
    │ │                           │    │
    │ │ 2. ZeroLineCrossover.     │    │
    │ │    Analyze()              │    │
    │ │    ├─ Calc MACD           │    │
    │ │    ├─ Find crossovers     │    │
    │ │    ├─ Calc RSI (confirm)  │    │
    │ │    └─ Return signal (Y/N) │    │
    │ │                           │    │
    │ └──────────┬───────────────┘    │
    │            │                    │
    │ ┌──────────▼───────────────┐    │
    │ │ If Signal Found:          │    │
    │ │ ┌─ Notify (Desktop)      │    │
    │ │ ├─ Record Trade (entry)  │    │
    │ │ └─ Log Signal            │    │
    │ └───────────────────────────┘    │
    │                                  │
    └──────────────────────────────────┘
                     │
    ┌────────────────▼────────────────┐
    │ Log Cycle Complete               │
    │ "✅ Check done - X signals found"│
    └──────────────────────────────────┘
```

### Weekly Report Generation (Monday 00:00)

```
┌──────────────────────────────┐
│ Weekly Timer Tick             │
│ (Monday 00:00 UTC)            │
└────────────────┬──────────────┘
                 │
    ┌────────────▼────────────┐
    │ Load all trades from     │
    │ Data/trades.json        │
    └────────────┬────────────┘
                 │
    ┌────────────▼────────────────┐
    │ Filter trades for this week  │
    └────────────┬────────────────┘
                 │
    ┌────────────▼────────────────┐
    │ Calculate Statistics:        │
    │ • Total trades              │
    │ • Closed trades             │
    │ • Open trades               │
    │ • Total profit              │
    │ • Avg ROI                   │
    │ • Win rate                  │
    │ • Best trade                │
    │ • Worst trade               │
    └────────────┬────────────────┘
                 │
    ┌────────────▼────────────────┐
    │ Generate Report Text         │
    │ (Beautiful formatted)        │
    └────────────┬────────────────┘
                 │
    ┌────────────▼────────────────┐
    │ Save to Data/Reports/        │
    │ Weekly_Report_YYYY-MM-DD.txt │
    └────────────┬────────────────┘
                 │
    ┌────────────▼────────────────┐
    │ Print to Console             │
    └──────────────────────────────┘
```

---

## Component Details

### 1. BotSchedulerService (Orchestrator)

**Responsabilità**:
- Avvia i timer
- Coordina i servizi
- Gestisce il ciclo principale

**Metodi Pubblici**:
- `StartAsync()` - Avvia il bot
- `Stop()` - Ferma il bot

**Metodi Privati**:
- `CheckMarketAsync()` - Ciclo analisi market
- Timer callbacks

**Dipendenze**:
- CryptoDataService
- BullishDivergenceStrategy
- ZeroLineCrossoverStrategy
- NotificationService
- ReportingService

---

### 2. CryptoDataService (Data Provider)

**Responsabilità**:
- Comunicare con Crypto.com API
- Parsing risposta JSON
- Conversione dati a oggetti

**Metodi Pubblici**:
```csharp
Task<List<Cryptocurrency>> GetLargeCapCryptocurrenciesAsync()
Task<List<Candle>> GetCandlesAsync(string symbol, string interval, int limit)
```

**API Endpoints**:
- `GET /public/get-ticker` → Cryptos attuali
- `GET /public/get-candlestick` → Candele OHLCV

**Error Handling**:
- Try-catch su tutte le operazioni HTTP
- Fallback a lista vuota su errore

---

### 3. Strategy Pattern

**Base Contract**:
```csharp
public AnalysisResult Analyze(string symbol, List<Candle> candles)
{
    // Implementazione specifica strategia
    // Ritorna AnalysisResult con segnale
}
```

**BullishDivergenceStrategy**:
- Calcola RSI
- Trova minimi locali nel prezzo
- Cerca divergenza con RSI
- Ritorna BUY se divergenza bullish trovata

**ZeroLineCrossoverStrategy**:
- Calcola MACD
- Rileva incroci sulla linea zero
- Opzionale: conferma con RSI
- Ritorna BUY/SELL su crossover

**Pattern Vantaggi**:
- Facile aggiungere nuove strategie
- Isolamento della logica
- Testing indipendente

---

### 4. TechnicalIndicators (Static Library)

**Indicatori Disponibili**:

1. **RSI (Relative Strength Index)**
   - Input: List<decimal> prices, int period
   - Output: List<decimal> RSI values
   - Uso: Momentum, overbought/oversold

2. **MACD (Moving Average Convergence Divergence)**
   - Input: List<decimal> prices, 3 periods
   - Output: (macd, signal, histogram)
   - Uso: Trend following, convergence/divergence

3. **EMA (Exponential Moving Average)**
   - Input: List<decimal> prices, int period
   - Output: List<decimal> EMA values
   - Uso: Trend smoothing, trend lines

4. **SMA (Simple Moving Average)**
   - Input: List<decimal> prices, int period
   - Output: List<decimal> SMA values
   - Uso: Trend, support/resistance

5. **Bollinger Bands**
   - Input: List<decimal>, period, stdDev multiplier
   - Output: (upper, middle, lower)
   - Uso: Volatility, mean reversion

---

### 5. NotificationService (Alerts)

**Canali Notifica**:
1. **Windows Toast**
   - PowerShell script
   - Visibile sul desktop
   - Suono di sistema

2. **Console Output**
   - Colorato (green/red)
   - Real-time
   - Beep (Console.Beep)

3. **File Logging**
   - `Logs/signals_YYYY-MM-DD.log`
   - Timestamp + Messaggio
   - Persistente

**Flusso**:
```
Signal Found
    ↓
FormatNotification()
    ↓
LogToFile() ─────→ Logs/signals_*.log
    ↓
ConsoleNotification() → Console output + Beep
    ↓
SendWindowsNotificationAsync() → Toast popup
```

---

### 6. ReportingService (Persistence)

**Data Model**:
```
Trade {
  Id: GUID
  Symbol: string
  OpenTime: DateTime
  EntryPrice: decimal
  CloseTime: DateTime?
  ExitPrice: decimal?
  Strategy: string
  Status: "Open" | "Closed"
  Profit: decimal?
  ProfitPercentage: decimal?
}
```

**Operazioni**:
1. `RecordTrade()` - Nuovo trade apertura
2. `CloseTrade()` - Chiude trade aperto
3. `GenerateWeeklyReport()` - Report settimanale
4. `GetAllTrades()` - Recupera tutti i trades

**Storage**:
- **File**: `Data/trades.json`
- **Format**: JSON array di Trade objects
- **Persistence**: Read/Write su ogni operazione

---

## Class Diagram

```
┌─────────────────────────────────┐
│       BotSchedulerService       │
├─────────────────────────────────┤
│ - _marketCheckTimer: Timer      │
│ - _weeklyReportTimer: Timer     │
│ - _dataService: CryptoDataService
│ - _notifyService: NotifyService │
│ - _reportService: ReportService │
│ - _bullishStrategy: Strategy    │
│ - _crossoverStrategy: Strategy  │
├─────────────────────────────────┤
│ + StartAsync(): Task            │
│ + Stop(): void                  │
│ - CheckMarketAsync(): Task      │
└─────────────────────────────────┘
          △     △      △
          │     │      │ implements
┌─────────┴─┐ ┌─┴──────┴─────────────────────┐
│ Service   │ │                              │
├───────────┤ │  ┌──────────────────────┐   │
│ Abstract  │ │  │ CryptoDataService    │   │
│ Contract  │ │  ├──────────────────────┤   │
└───────────┘ │  │ HttpClient           │   │
              │  ├──────────────────────┤   │
              │  │ + GetLargeCap...()   │   │
              │  │ + GetCandles...()    │   │
              │  └──────────────────────┘   │
              │                              │
              │  ┌──────────────────────┐   │
              │  │ NotificationService  │   │
              │  ├──────────────────────┤   │
              │  │ + Send...()          │   │
              │  │ - LogToFile()        │   │
              │  │ - Console...()       │   │
              │  │ - SendWindows...()   │   │
              │  └──────────────────────┘   │
              │                              │
              │  ┌──────────────────────┐   │
              │  │ ReportingService     │   │
              │  ├──────────────────────┤   │
              │  │ + RecordTrade()      │   │
              │  │ + CloseTrade()       │   │
              │  │ + GenerateWeekly...()│   │
              │  │ + GetAllTrades()     │   │
              │  └──────────────────────┘   │
              └──────────────────────────────┘
```

---

## Security Model

```
┌────────────────────────────────────────┐
│         SECURITY PERIMETER             │
│                                        │
│  ✅ API Read-Only (no trading)         │
│  ✅ No credentials stored              │
│  ✅ Local file storage only            │
│  ✅ No external data upload            │
│  ✅ No user tracking                   │
│                                        │
│  🔒 Isolamento:                        │
│  ✅ Process-local (single-threaded)    │
│  ✅ No network except API              │
│  ✅ No shell commands executed         │
│  ✅ Safe JSON parsing                  │
│                                        │
└────────────────────────────────────────┘
```

---

## Performance Characteristics

```
┌──────────────────────────────────────┐
│         Performance Profile          │
├──────────────────────────────────────┤
│ Memory Usage:        50-100 MB       │
│ CPU Usage:           Low (~2-5%)     │
│ Network Bandwidth:   1-2 MB / cycle  │
│ Cycle Duration:      ~20-30 seconds  │
│ API Rate:            50 req/min max  │
│ Delay between cryptos: 100 ms        │
│ Storage Growth:      ~1 MB/month     │
└──────────────────────────────────────┘
```

---

## Extension Points

### Adding New Strategy

1. Crea file in `Strategies/`
2. Implementa `Analyze()` method
3. Aggiungi in `BotSchedulerService`
4. Chiama in `CheckMarketAsync()`

### Adding New Indicator

1. Crea metodo in `TechnicalIndicators.cs`
2. Firma: `List<decimal> Calculate<Name>(...)`
3. Usa nelle strategie

### Adding New Notification Channel

1. Crea metodo in `NotificationService.cs`
2. Chiama da `SendNotificationAsync()`
3. Test con scenario reale

### Adding New Report Type

1. Crea metodo in `ReportingService.cs`
2. Chiama da timer o on-demand
3. Salva in `Data/Reports/`

---

## Threading Model

```
Main Thread
├─ Timer 1: Market Check (5 min)
│  ├─ CryptoDataService.GetTickers() [Async]
│  ├─ For Each Crypto:
│  │  ├─ GetCandlesAsync() [Async]
│  │  ├─ Strategy.Analyze() [Sync]
│  │  ├─ SendNotificationAsync() [Async]
│  │  └─ RecordTrade() [Sync]
│  └─ Delay 100ms per crypto
│
└─ Timer 2: Weekly Report (7 days)
   └─ GenerateWeeklyReport() [Sync]

All operations are I/O-bound and await-friendly
```

---

## Configuration

Configuration centralized in `config.json`:

```json
{
  "monitoring": {
    "intervalMinutes": 5,
    "maxCryptocurrenciesToAnalyze": 50,
    "candleTimeframe": "4h",
    "candlesLookback": 100
  },
  "strategies": {
    "bullishDivergence": {
      "rsiPeriod": 14,
      "lookbackPeriods": 5
    },
    "zeroLineCrossover": {
      "macdFastPeriod": 12,
      "macdSlowPeriod": 26,
      "macdSignalPeriod": 9
    }
  }
}
```

---

## Scalability Considerations

**Attuali Limiti**:
- Single machine only
- File-based database
- 50 cryptos per cycle
- 5-minute interval minimum

**Improvement Path**:
- v1.1: SQL database backend
- v1.2: Multi-machine clustering
- v1.3: Real trading execution
- v1.4: Web API frontend

---

*Architecture documento | Bot Cripto v1.0 | .NET 10 C#*
