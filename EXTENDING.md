# 🔧 Estensione del Bot - Guida per Sviluppatori

Questa guida spiega come aggiungere nuove strategie e funzionalità al bot.

## Aggiungere una Nuova Strategia

### Passo 1: Creare la Classe della Strategia

Crea un nuovo file in `Strategies/` seguendo il pattern:

```csharp
using BotCripto.Models;

namespace BotCripto.Strategies;

public class MiaStrategyStrategy
{
    public AnalysisResult Analyze(string symbol, List<Candle> candles)
    {
        var result = new AnalysisResult
        {
            Symbol = symbol,
            StrategyName = "Mia Strategy",
            IsSignal = false,
            Signal = "No Signal",
            AnalysisTime = DateTime.UtcNow
        };

        if (candles.Count < 20)
            return result;

        // Implementa la logica della strategia
        var closes = candles.Select(c => c.Close).ToList();
        
        // Esempio: Usa gli indicatori disponibili
        var rsi = TechnicalIndicators.CalculateRSI(closes);
        var sma = TechnicalIndicators.CalculateSMA(closes, 20);

        // Logica di trading
        if (/* condizione del segnale */)
        {
            result.IsSignal = true;
            result.Signal = "BUY - Mia Strategy Detected";
            result.Indicators["CustomIndicator"] = /* valore */;
        }

        return result;
    }
}
```

### Passo 2: Integrare nel BotSchedulerService

Modifica `Services/BotSchedulerService.cs`:

```csharp
private readonly MiaStrategyStrategy _miaStrategy;

public BotSchedulerService()
{
    _dataService = new CryptoDataService();
    _notificationService = new NotificationService();
    _reportingService = new ReportingService();
    _bullishStrategy = new BullishDivergenceStrategy();
    _crossoverStrategy = new ZeroLineCrossoverStrategy();
    _miaStrategy = new MiaStrategyStrategy();  // Aggiungi questa riga
}

private async Task CheckMarketAsync()
{
    // ... codice esistente ...
    
    // Aggiungi questa sezione nella loop di analisi
    var miaResult = _miaStrategy.Analyze(crypto.Symbol, candles);
    if (miaResult.IsSignal && !bullishResult.IsSignal && !crossoverResult.IsSignal)
    {
        await _notificationService.SendNotificationAsync(miaResult);
        _reportingService.RecordTrade(crypto.Symbol, crypto.CurrentPrice, "Mia Strategy");
        signalsFound++;
    }
}
```

## Indicatori Tecnici Disponibili

Tutti gli indicatori si trovano in `Strategies/TechnicalIndicators.cs`:

### RSI (Relative Strength Index)
```csharp
var rsi = TechnicalIndicators.CalculateRSI(closes, period: 14);
```

### MACD (Moving Average Convergence Divergence)
```csharp
var (macd, signal, histogram) = TechnicalIndicators.CalculateMACD(
    closes, 
    fastPeriod: 12, 
    slowPeriod: 26, 
    signalPeriod: 9
);
```

### EMA (Exponential Moving Average)
```csharp
var ema = TechnicalIndicators.CalculateEMA(closes, period: 12);
```

### SMA (Simple Moving Average)
```csharp
var sma = TechnicalIndicators.CalculateSMA(closes, period: 20);
```

### Bollinger Bands
```csharp
var (upper, middle, lower) = TechnicalIndicators.CalculateBollingerBands(
    closes, 
    period: 20, 
    stdDevMultiplier: 2
);
```

## Aggiungere Nuovi Indicatori

Aggiungi il tuo indicatore a `Strategies/TechnicalIndicators.cs`:

```csharp
public static List<decimal> CalculateMioIndicatore(
    List<decimal> prices, 
    int period = 14)
{
    var result = new List<decimal>();
    
    if (prices.Count < period)
        return result;
    
    // Implementa il calcolo dell'indicatore
    for (int i = period - 1; i < prices.Count; i++)
    {
        var valore = /* calcolo */;
        result.Add(valore);
    }
    
    return result;
}
```

## Modificare i Servizi

### Customizzare le Notifiche

Modifica `Services/NotificationService.cs` per cambiare il formato o il canale:

```csharp
public async Task SendNotificationAsync(AnalysisResult result)
{
    // Aggiungi il tuo tipo di notifica (email, Slack, webhook, etc.)
    await SendToSlackAsync(result);
    await SendToEmailAsync(result);
    
    // Notifiche di default
    LogToFile(FormatNotification(result));
    ConsoleNotification(result);
}
```

### Estendere il Reporting

Modifica `Services/ReportingService.cs` per aggiungere metriche:

```csharp
public void GenerateWeeklyReport()
{
    // Codice di calcolo statistiche...
    
    var avgWinSize = closedTrades
        .Where(t => t.Profit > 0)
        .Average(t => t.Profit);
    
    var avgLossSize = closedTrades
        .Where(t => t.Profit <= 0)
        .Average(t => t.Profit);
    
    // Aggiungi metriche personalizzate al report
    report.AppendLine($"Avg Win: ${avgWinSize:F2}");
    report.AppendLine($"Avg Loss: ${avgLossSize:F2}");
}
```

## Pattern di Condizione Trading

### Trend Following
```csharp
var sma20 = TechnicalIndicators.CalculateSMA(closes, 20);
var sma50 = TechnicalIndicators.CalculateSMA(closes, 50);

if (sma20.Last() > sma50.Last())
    result.Signal = "BUY - Bullish Trend";
```

### Mean Reversion
```csharp
var (upper, middle, lower) = TechnicalIndicators.CalculateBollingerBands(closes);

if (closes.Last() < lower.Last())
    result.Signal = "BUY - Price Below Lower Band";
```

### Momentum
```csharp
var rsi = TechnicalIndicators.CalculateRSI(closes);

if (rsi.Last() > 70)
    result.Signal = "SELL - Overbought";
else if (rsi.Last() < 30)
    result.Signal = "BUY - Oversold";
```

### Confluence (Multipli Indicatori)
```csharp
var rsi = TechnicalIndicators.CalculateRSI(closes);
var (macd, signal, _) = TechnicalIndicators.CalculateMACD(closes);
var sma = TechnicalIndicators.CalculateSMA(closes, 20);

if (rsi.Last() < 30 && 
    macd.Last() < signal.Last() && 
    closes.Last() < sma.Last())
{
    result.Signal = "STRONG BUY - Multiple Confirmations";
}
```

## Testing delle Strategie

Per testare una nuova strategia prima di attivarla:

1. **Backup temporaneo**: Commenta la registrazione della strategia in `BotSchedulerService.cs`
2. **Unit test**: Crea test con dati storici
3. **Paper trading**: Attiva solo il logging, senza transazioni reali
4. **Validazione**: Verifica i risultati nel report settimanale

## Struttura AnalysisResult

```csharp
public class AnalysisResult
{
    public string Symbol { get; set; }                    // es: "BTC"
    public string StrategyName { get; set; }              // es: "Bullish Divergence"
    public bool IsSignal { get; set; }                    // Signal trovato?
    public string Signal { get; set; }                    // "BUY" o "SELL"
    public decimal CurrentPrice { get; set; }             // Prezzo attuale
    public DateTime AnalysisTime { get; set; }            // Quando è stato analizzato
    public Dictionary<string, decimal> Indicators { get; set; }  // Valori indicatori
}
```

## Performance Tips

1. **Limita le analisi**: Aumenta l'intervallo di monitoraggio se CPU è alta
2. **Cache dati**: Riutilizza le candele per più strategie
3. **Parallelize**: Usa `Parallel.ForEach` per analizzare multiple cripto
4. **Ottimizza indicatori**: Calcola una sola volta e riusa

```csharp
// Cattivo - Calcola RSI 3 volte
var rsi1 = CalculateRSI(closes);
var rsi2 = CalculateRSI(closes);
var rsi3 = CalculateRSI(closes);

// Buono - Calcola una sola volta
var rsi = CalculateRSI(closes);
var rsiStd = CalculateStdDev(rsi);
var rsiMA = CalculateSMA(rsi.Cast<decimal>().ToList(), 5);
```

---

**Suggerimento**: Documenta sempre le tue strategie nel codice con commenti sul WHY, non sul WHAT!
