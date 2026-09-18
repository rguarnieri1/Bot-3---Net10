using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using BotCripto.Models;
using InterReact;

namespace BotCripto.Services;

public class InteractiveBrokersDataService
{
    private IInterReactClient? _ibClient;
    private string _sessionId = "";
    private readonly string _accountId = "";
    private DateTime _lastApiCallTime = DateTime.UtcNow;
    private const int RateLimitDelayMs = 50;
    private bool _useRealtimeMode = true;
    private Dictionary<string, decimal> _priceCache = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private string _currentDataSource = "InteractiveBrokers"; // Traccia la fonte dati attuale

    // Simboli del mercato italiano predefiniti
    private readonly List<string> _italianMarketSymbols = new()
    {
        "ENI",          // Eni
        "ISP",          // Intesa Sanpaolo
        "UCG",          // UniCredit
        "TIT",          // Telecom Italia
        "BAMI",         // Banco di Napoli
        "BPE",          // Banca Popolare dell'Emilia Romagna
        "STM",          // STMicroelectronics
        "ENEL",         // Enel
        "AZM",          // Azionario Mediobanca
        "FTSEMIB",      // FTSE MIB Index
        "EQNR",         // Equinor
        "EXS2",         // ETF iShares MSCI World
        "VWRL",         // Vanguard FTSE World
        "MICC",         // Mediobanca
        "UNL"           // Unilever
    };

    public InteractiveBrokersDataService(string accountId = "")
    {
        _accountId = accountId;
    }

    public async Task<bool> ConnectAsync(string username, string password)
    {
        try
        {
            Console.WriteLine("🔌 Tentativo connessione Interactive Brokers TWS (porta 7497)...");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            _ibClient = await InterReactClient.ConnectAsync(o =>
            {
                o.TwsIpAddress = IPAddress.Loopback;
                o.IBPortAddresses = new[] { 7497 };
            }, cts.Token);

            // Usa dati differiti se il conto non ha una sottoscrizione real-time per il titolo richiesto
            // (viene ignorato automaticamente quando i dati real-time sono disponibili).
            _ibClient.Request.RequestMarketDataType(MarketDataType.Delayed);

            // Logga gli alert di TWS (es. contratto non trovato, sottoscrizione dati mancante)
            // per poter diagnosticare perché una richiesta non restituisce dati.
            _ibClient.Response
                .OfType<AlertMessage>()
                .Subscribe(a => Console.WriteLine($"   ℹ️  IB Alert [{a.Code}]: {a.Message}"));

            Console.WriteLine($"✅ Connessione TWS ATTIVA su {_ibClient.RemoteIpEndPoint} (Real-Time)");
            _useRealtimeMode = true;
            _sessionId = "tws_direct_socket";
            _currentDataSource = "InteractiveBrokers (TWS Real-Time)";
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TWS non disponibile sulla porta 7497: {ex.Message}");
            Console.WriteLine("📝 Assicurati che:");
            Console.WriteLine("   1. TWS sia aperto e in esecuzione");
            Console.WriteLine("   2. API sia abilitata: Settings → API → Settings → Enable ActiveX and Socket Clients");
            Console.WriteLine("   3. La porta 7497 sia accessibile");
            Console.WriteLine("⚠️  ATTENZIONE: Sistema operando in modalità DEMO (nessun fallback disponibile)");
            _ibClient = null;
            _useRealtimeMode = false;
            _sessionId = "demo_only";
            _currentDataSource = "Demo Data";
            return false;
        }
    }

    public async Task<List<Cryptocurrency>> GetItalianStocksAsync()
    {
        var result = new List<Cryptocurrency>();

        foreach (var symbol in _italianMarketSymbols)
        {
            try
            {
                var crypto = await GetItalianStockAsync(symbol);
                if (crypto != null)
                {
                    result.Add(crypto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  {symbol}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<Cryptocurrency> GetItalianStockAsync(string symbol)
    {
        try
        {
            await RateLimitDelay();

            // Formato IB: Per borsa italiana aggiungi .MI al simbolo
            var ibSymbol = symbol.Contains(".") ? symbol : $"{symbol}.MI";

            var price = await GetMarketPriceAsync(ibSymbol);

            if (price > 0)
            {
                return new Cryptocurrency
                {
                    Symbol = symbol,
                    Name = GetItalianStockName(symbol),
                    CurrentPrice = price,
                    LastUpdate = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            // Log errore silenziosamente
        }

        return null;
    }

    private async Task<decimal> GetMarketPriceAsync(string ibSymbol)
    {
        try
        {
            // Caching intelligente per real-time
            if (_useRealtimeMode && _priceCache.ContainsKey(ibSymbol))
            {
                var cacheAge = (DateTime.UtcNow - _lastCacheUpdate).TotalSeconds;
                if (cacheAge < 2) // Cache valido per 2 secondi
                {
                    return _priceCache[ibSymbol];
                }
            }

            // PRIMO TENTATIVO: Interactive Brokers TWS (PRIMARIA)
            if (_sessionId == "tws_direct_socket" && _ibClient != null)
            {
                var price = await GetPriceFromTwsAsync(ibSymbol);
                if (price > 0)
                {
                    _priceCache[ibSymbol] = price;
                    _lastCacheUpdate = DateTime.UtcNow;
                    Console.WriteLine($"✅ {ibSymbol}: €{price:F2} (IB TWS - Real-Time)");
                    return price;
                }
                else
                {
                    Console.WriteLine($"⚠️  {ibSymbol}: TWS non ha dati, usando demo...");
                }
            }

            // ULTIMO RICORSO: Demo Data
            Console.WriteLine($"⚠️  {ibSymbol}: Usando prezzo demo (fallback finale)");
            return GetDemoPrice(ibSymbol);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Errore prezzo {ibSymbol}: {ex.Message}");
            return -1m; // Ritorna -1 per errore
        }
    }

    private async Task<decimal> GetPriceFromTwsAsync(string ibSymbol)
    {
        if (_ibClient == null)
            return -1m;

        try
        {
            var contract = BuildItalianContract(ibSymbol);

            IHasRequestId[] snapshot = await _ibClient.Service.GetMarketDataSnapshotAsync(
                contract,
                timeout: TimeSpan.FromSeconds(8));

            var priceTicks = snapshot.OfTickClass(s => s.PriceTick).ToList();

            var tick = priceTicks.FirstOrDefault(t => t.TickType == TickType.LastPrice && t.Price > 0)
                ?? priceTicks.FirstOrDefault(t => t.TickType == TickType.ClosePrice && t.Price > 0);

            return tick != null ? (decimal)tick.Price : -1m;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  TWS market data ({ibSymbol}): {ex.Message}");
            return -1m;
        }
    }

    private decimal GetDemoPrice(string ibSymbol)
    {
        return ibSymbol switch
        {
            "ENI.MI" => 14.85m,
            "ISP.MI" => 3.45m,
            "UCG.MI" => 35.92m,
            "TIT.MI" => 0.3185m,
            "BAMI.MI" => 8.65m,
            "BPE.MI" => 6.78m,
            "STM.MI" => 34.50m,
            "ENEL.MI" => 6.42m,
            "AZM.MI" => 43.21m,
            "FTSEMIB.MIX" => 34567.89m,
            "EQNR.MI" => 28.75m,
            "EXS2.MI" => 65.43m,
            "VWRL.MI" => 87.65m,
            "MICC.MI" => 15.32m,
            "UNL.MI" => 58.92m,
            _ => -1m
        };
    }

    // Contratto IB per un titolo del mercato italiano (Borsa Italiana / Euronext Milan).
    // FTSEMIB è un indice e richiede un routing diverso da SMART.
    private Contract BuildItalianContract(string ibSymbol)
    {
        var bareSymbol = ibSymbol.Split('.')[0];
        var isIndex = bareSymbol == "FTSEMIB";

        return new Contract
        {
            SecurityType = isIndex ? ContractSecurityType.Index : ContractSecurityType.Stock,
            Symbol = bareSymbol,
            Currency = "EUR",
            Exchange = isIndex ? "BVME" : "SMART",
            PrimaryExchange = isIndex ? "" : "BVME"
        };
    }

    private string GetItalianStockName(string symbol)
    {
        return symbol switch
        {
            "ENI" => "Eni SpA",
            "ISP" => "Intesa Sanpaolo",
            "UCG" => "UniCredit",
            "TIT" => "Telecom Italia",
            "BAMI" => "Banco di Napoli",
            "BPE" => "Banca Popolare dell'Emilia",
            "STM" => "STMicroelectronics",
            "ENEL" => "Enel SpA",
            "AZM" => "Mediobanca",
            "FTSEMIB" => "FTSE MIB Index",
            "EQNR" => "Equinor",
            "EXS2" => "iShares MSCI World",
            "VWRL" => "Vanguard FTSE World",
            "MICC" => "Mediobanca",
            "UNL" => "Unilever",
            _ => symbol
        };
    }

    public async Task<List<Candle>> GetCandlesAsync(string symbol, string interval = "1h", int limit = 100)
    {
        try
        {
            await RateLimitDelay();

            var ibSymbol = symbol.Contains(".") ? symbol : $"{symbol}.MI";

            // Prova a ottenere candele reali da IB
            if (!string.IsNullOrEmpty(_sessionId))
            {
                var realCandles = await GetRealCandlesFromIbAsync(ibSymbol, interval, limit);
                if (realCandles != null && realCandles.Count >= 50)
                {
                    Console.WriteLine($"✅ {ibSymbol}: {realCandles.Count} candele caricate da IB");
                    return realCandles;
                }
            }

            // Fallback a dati demo se API non disponibile
            Console.WriteLine($"⚠️  Uso candele demo per {ibSymbol}");
            return GenerateDemoCandles(symbol, limit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore candele {symbol}: {ex.Message}");
            return GenerateDemoCandles(symbol, limit);
        }
    }

    private async Task<List<Candle>> GetRealCandlesFromIbAsync(string ibSymbol, string interval, int limit)
    {
        try
        {
            // Usa SOLO connessione TWS diretta
            if (_sessionId != "tws_direct_socket" || _ibClient == null)
            {
                return null;
            }

            var candles = await GetCandlesFromTwsAsync(ibSymbol, interval, limit);
            if (candles != null && candles.Count >= 50)
            {
                return candles;
            }

            // Se fallisce, ritorna null per usare demo
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<Candle>> GetCandlesFromTwsAsync(string ibSymbol, string interval, int limit)
    {
        if (_ibClient == null)
            return null;

        try
        {
            var contract = BuildItalianContract(ibSymbol);
            var barSize = ConvertIntervalToBarSize(interval);
            var requestId = _ibClient.Request.GetNextId();

            var historicalDataTask = _ibClient.Response
                .OfType<HistoricalData>()
                .Where(h => h.RequestId == requestId)
                .Timeout(TimeSpan.FromSeconds(15))
                .FirstAsync()
                .ToTask();

            _ibClient.Request.RequestHistoricalData(
                requestId,
                contract,
                endDateTime: "",
                duration: HistoricalDataDuration.OneMonth,
                barSize: barSize,
                whatToShow: HistoricalDataWhatToShow.Trades,
                regularTradingHoursOnly: true,
                dateFormat: 2, // secondi Unix, più semplice da convertire
                keepUpToDate: false);

            var historicalData = await historicalDataTask;

            return historicalData.Bars
                .Select(bar => new Candle
                {
                    Time = UnixTimeStampToDateTime(long.Parse(bar.Time)),
                    Open = (decimal)bar.Open,
                    High = (decimal)bar.High,
                    Low = (decimal)bar.Low,
                    Close = (decimal)bar.Close,
                    Volume = bar.Volume
                })
                .OrderBy(c => c.Time)
                .TakeLast(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  TWS dati storici ({ibSymbol}): {ex.Message}");
            return null;
        }
    }

    private string ConvertIntervalToBarSize(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => HistoricalDataBarSize.OneMinute,
            "5m" => HistoricalDataBarSize.FiveMinutes,
            "15m" => HistoricalDataBarSize.FifteenMinutes,
            "30m" => HistoricalDataBarSize.ThirtyMinutes,
            "1h" => HistoricalDataBarSize.OneHour,
            "4h" => HistoricalDataBarSize.FourHours,
            "1d" => HistoricalDataBarSize.OneDay,
            _ => HistoricalDataBarSize.OneHour
        };
    }

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    public List<string> GetConfiguredSymbols()
    {
        return _italianMarketSymbols;
    }

    public void AddSymbol(string symbol)
    {
        if (!_italianMarketSymbols.Contains(symbol))
        {
            _italianMarketSymbols.Add(symbol);
            Console.WriteLine($"➕ Simbolo aggiunto: {symbol}");
        }
    }

    public string GetDataSourceStatus()
    {
        var status = $"📊 Data Source: {_currentDataSource}";

        if (_sessionId == "tws_direct_socket")
        {
            status += " [PRIMARY]";
        }

        return status;
    }

    private List<Candle> GenerateDemoCandles(string symbol, int limit)
    {
        var candles = new List<Candle>();
        var basePrice = GetSymbolBasePrice(symbol);
        var random = new Random();

        for (int i = limit; i > 0; i--)
        {
            var time = DateTime.UtcNow.AddHours(-i);
            var volatility = basePrice * 0.01m;
            var open = basePrice + (decimal)(random.NextDouble() - 0.5) * (volatility * 2);
            var close = open + (decimal)(random.NextDouble() - 0.5) * volatility;
            var high = Math.Max(open, close) + (decimal)random.NextDouble() * volatility;
            var low = Math.Min(open, close) - (decimal)random.NextDouble() * volatility;
            var volume = 500000m + (decimal)(random.NextDouble() * 4500000);

            candles.Add(new Candle
            {
                Time = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });

            basePrice = close;
        }

        return candles.OrderBy(c => c.Time).ToList();
    }

    private decimal GetSymbolBasePrice(string symbol)
    {
        return symbol switch
        {
            "ENI" => 14.85m,
            "ISP" => 3.45m,
            "UCG" => 35.92m,
            "TIT" => 0.3185m,
            "BAMI" => 8.65m,
            "BPE" => 6.78m,
            "STM" => 34.50m,
            "ENEL" => 6.42m,
            "AZM" => 43.21m,
            "FTSEMIB" => 34567.89m,
            "EQNR" => 28.75m,
            "EXS2" => 65.43m,
            "VWRL" => 87.65m,
            "MICC" => 15.32m,
            "UNL" => 58.92m,
            _ => 10m
        };
    }

    private async Task RateLimitDelay()
    {
        var timeSinceLastCall = (DateTime.UtcNow - _lastApiCallTime).TotalMilliseconds;
        if (timeSinceLastCall < RateLimitDelayMs)
        {
            await Task.Delay((int)(RateLimitDelayMs - timeSinceLastCall));
        }
        _lastApiCallTime = DateTime.UtcNow;
    }
}
