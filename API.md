# 🔌 API Documentation - Bot Cripto

Documentazione tecnica per l'integrazione con Crypto.com Exchange API e servizi interni.

## Servizi Disponibili

### 1. CryptoDataService

Gestisce il recupero dei dati da Crypto.com.

#### GetLargeCapCryptocurrenciesAsync()
```csharp
public async Task<List<Cryptocurrency>> GetLargeCapCryptocurrenciesAsync()
```

**Descrizione**: Recupera il ticker di tutte le criptovalute disponibili con > $1B market cap

**Returns**: Lista di `Cryptocurrency` objects
```csharp
public class Cryptocurrency {
    public string Symbol { get; set; }              // "BTC", "ETH"
    public string Name { get; set; }                // Nome completo
    public decimal MarketCap { get; set; }          // Capitalizzazione
    public decimal CurrentPrice { get; set; }       // Prezzo attuale
    public DateTime LastUpdate { get; set; }        // Timestamp
}
```

**Esempio di utilizzo**:
```csharp
var service = new CryptoDataService();
var cryptos = await service.GetLargeCapCryptocurrenciesAsync();

foreach (var crypto in cryptos)
{
    Console.WriteLine($"{crypto.Symbol}: ${crypto.CurrentPrice}");
}
```

#### GetCandlesAsync()
```csharp
public async Task<List<Candle>> GetCandlesAsync(
    string symbol, 
    string interval = "1h", 
    int limit = 100)
```

**Parametri**:
- `symbol`: Simbolo cripto (es: "BTC", "ETH")
- `interval`: Timeframe della candela (default: "1h")
  - "1m": 1 minuto
  - "5m": 5 minuti
  - "15m": 15 minuti
  - "30m": 30 minuti
  - "1h": 1 ora
  - "4h": 4 ore
  - "1d": 1 giorno
- `limit`: Numero di candele (default: 100, max: 200)

**Returns**: Lista di `Candle` objects
```csharp
public class Candle {
    public DateTime Time { get; set; }      // Orario apertura candela
    public decimal Open { get; set; }       // Prezzo apertura
    public decimal High { get; set; }       // Prezzo massimo
    public decimal Low { get; set; }        // Prezzo minimo
    public decimal Close { get; set; }      // Prezzo chiusura
    public decimal Volume { get; set; }     // Volume scambi
}
```

**Esempio di utilizzo**:
```csharp
var candles = await service.GetCandlesAsync("BTC", "4h", 50);

foreach (var candle in candles)
{
    Console.WriteLine(
        $"{candle.Time:yyyy-MM-dd HH:mm} - " +
        $"O:{candle.Open} H:{candle.High} L:{candle.Low} C:{candle.Close}");
}
```

---

### 2. NotificationService

Gestisce le notifiche desktop e il logging.

#### SendNotificationAsync()
```csharp
public async Task SendNotificationAsync(AnalysisResult result)
```

**Descrizione**: Invia una notifica desktop e registra il segnale

**Parametri**:
- `result`: `AnalysisResult` contenente il segnale da notificare

**Effetti collaterali**:
- Notifica Windows Toast
- Sound alert (beep)
- Log a file in `Logs/signals_YYYY-MM-DD.log`
- Output a console

**Esempio di utilizzo**:
```csharp
var notificationService = new NotificationService();
var result = new AnalysisResult
{
    Symbol = "BTC",
    Signal = "BUY - Bullish Divergence Detected",
    CurrentPrice = 42500
};

await notificationService.SendNotificationAsync(result);
```

---

### 3. ReportingService

Gestisce il tracking delle operazioni e il reporting.

#### RecordTrade()
```csharp
public void RecordTrade(
    string symbol, 
    decimal entryPrice, 
    string strategy)
```

**Descrizione**: Registra una nuova operazione in apertura

**Parametri**:
- `symbol`: Simbolo cripto (es: "BTC")
- `entryPrice`: Prezzo di ingresso
- `strategy`: Nome della strategia (es: "Bullish Divergence")

**Esempio di utilizzo**:
```csharp
var reportingService = new ReportingService();
reportingService.RecordTrade("BTC", 42500.50m, "Bullish Divergence");
```

#### CloseTrade()
```csharp
public void CloseTrade(string symbol, decimal exitPrice)
```

**Descrizione**: Chiude un'operazione aperta

**Parametri**:
- `symbol`: Simbolo cripto da chiudere
- `exitPrice`: Prezzo di uscita

**Effetti collaterali**:
- Calcola automaticamente Profit e ProfitPercentage
- Aggiorna lo status a "Closed"
- Salva il trades.json aggiornato

**Esempio di utilizzo**:
```csharp
reportingService.CloseTrade("BTC", 43000.00m);
```

#### GenerateWeeklyReport()
```csharp
public void GenerateWeeklyReport()
```

**Descrizione**: Genera un report settimanale con statistiche

**Output**: 
- File salvato in `Data/Reports/Weekly_Report_YYYY-MM-DD.txt`
- Output anche a console

**Statistiche incluse**:
- Numero operazioni totali, chiuse, aperte
- Profitto totale e ROI medio
- Win rate
- Dettagli ogni operazione

#### GetAllTrades()
```csharp
public List<Trade> GetAllTrades()
```

**Returns**: Lista di tutte le operazioni registrate

```csharp
public class Trade {
    public string Id { get; set; }              // GUID unico
    public string Symbol { get; set; }          // "BTC"
    public DateTime OpenTime { get; set; }      // Quando aperto
    public DateTime? CloseTime { get; set; }    // Quando chiuso
    public decimal EntryPrice { get; set; }     // Prezzo ingresso
    public decimal? ExitPrice { get; set; }     // Prezzo uscita
    public string Strategy { get; set; }        // Strategia usata
    public string Status { get; set; }          // "Open" o "Closed"
    public decimal? Profit { get; set; }        // Profitto in $
    public decimal? ProfitPercentage { get; set; } // Profitto %
}
```

---

## Indicatori Tecnici

Vedi [Extending.md - Indicatori Tecnici Disponibili](EXTENDING.md#indicatori-tecnici-disponibili)

---

## AnalysisResult

Struttura dati per il risultato dell'analisi strategica.

```csharp
public class AnalysisResult {
    public string Symbol { get; set; }                              // "BTC"
    public string StrategyName { get; set; }                        // "Bullish Divergence"
    public bool IsSignal { get; set; }                              // Segnale trovato?
    public string Signal { get; set; }                              // Descrizione del segnale
    public decimal CurrentPrice { get; set; }                       // Prezzo attuale
    public DateTime AnalysisTime { get; set; }                      // Quando analizzato
    public Dictionary<string, decimal> Indicators { get; set; }     // Valori indicatori
}
```

**Esempio**:
```csharp
var result = new AnalysisResult
{
    Symbol = "BTC",
    StrategyName = "Bullish Divergence",
    IsSignal = true,
    Signal = "BUY - Bullish Divergence Detected",
    CurrentPrice = 42500.50m,
    AnalysisTime = DateTime.UtcNow,
    Indicators = new()
    {
        { "RSI", 35 },
        { "RSI_Prev", 32 },
        { "Divergence_Strength", 0.094m }
    }
};
```

---

## Flussi di Lavoro Comuni

### Analizzare una Cripto

```csharp
var dataService = new CryptoDataService();
var notifyService = new NotificationService();
var strategyService = new YourStrategy();

// 1. Recupera i dati
var candles = await dataService.GetCandlesAsync("BTC", "4h", 100);

// 2. Analizza con la strategia
var result = strategyService.Analyze("BTC", candles);

// 3. Se segnale trovato, notifica
if (result.IsSignal)
{
    await notifyService.SendNotificationAsync(result);
}
```

### Tracking di un Trade

```csharp
var reportingService = new ReportingService();

// Registra apertura
reportingService.RecordTrade("BTC", 42500.50m, "Bullish Divergence");

// ... Il bot rimane attivo...

// Chiudi quando le condizioni cambiano
reportingService.CloseTrade("BTC", 43000.00m);

// Genera report settimanale
reportingService.GenerateWeeklyReport();
```

---

## Configurazione API Crypto.com

### Endpoint Base
```
https://api.crypto.com/v2
```

### Endpoint Utilizzati

#### GET /public/get-ticker
Recupera il ticker di tutti gli strumenti

**Response Format**:
```json
{
  "result": {
    "data": [
      {
        "k": "BTC_USDT",
        "a": "42500.50"
      }
    ]
  }
}
```

#### GET /public/get-candlestick
Recupera le candele OHLCV

**Parametri Query**:
- `instrument_name`: es "BTC_USDT"
- `timeframe`: es "4h"
- `limit`: numero candele

**Response Format**:
```json
{
  "result": {
    "data": [
      [1693,41000,42000,40500,41500,1000]
    ]
  }
}
```

---

## Error Handling

Tutti i servizi hanno try-catch integrati:

```csharp
try
{
    var candles = await dataService.GetCandlesAsync("BTC");
}
catch (Exception ex)
{
    Console.WriteLine($"Errore: {ex.Message}");
    // Bot continua con la prossima cripto
}
```

**Errori comuni**:
- Network timeout → Riprova automaticamente
- API rate limit → Riduce il numero di richieste
- Dati malformati → Log dell'errore e skip

---

## Performance Tips

1. **Cache delle candele**: Riusa le candele per più strategie
2. **Batch requests**: Analizza multiple cripto in parallelo
3. **Connection pooling**: HttpClient è reusato
4. **Lazy evaluation**: Gli indicatori sono calcolati on-demand

---

## Rate Limiting

Crypto.com API ha limite di rate:
- ~50 richieste per minuto

Il bot gestisce automaticamente i delay:
```csharp
await Task.Delay(100); // 100ms tra una cripto e l'altra
```

---

## Logging

Tutti gli eventi sono loggati:
- `Logs/signals_YYYY-MM-DD.log` - Segnali trovati
- Console output - Real-time updates
- `Data/trades.json` - Operazioni salvate

---

Per ulteriori domande, vedi [EXTENDING.md](EXTENDING.md)
