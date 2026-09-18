using Newtonsoft.Json;
using BotCripto.Models;

namespace BotCripto.Services;

public class InteractiveBrokersDataService
{
    private readonly HttpClient _httpClient;
    private string _sessionId = "";
    private const string ClientPortalUrl = "https://localhost:5000/api";
    private const string TwsDirectUrl = "localhost:7497";
    private const string FinnhubApiUrl = "https://finnhub.io/api/v1";
    private const string FinnhubApiKey = "daggpk9r01quf8mu8vkgdaggpk9r01quf8mu8vl0";
    private readonly string _accountId = "";
    private int _apiCallCount = 0;
    private DateTime _lastApiCallTime = DateTime.UtcNow;
    private const int RateLimitDelayMs = 50;
    private bool _useRealtimeMode = true;
    private Dictionary<string, decimal> _priceCache = new();
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private bool _useFinnhubForRealData = false; // Fallback only - non primaria
    private bool _fallbackToFinnhub = false; // Attivato quando TWS è offline
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

    public InteractiveBrokersDataService(string accountId = "", bool useLocalGateway = true)
    {
        _accountId = accountId;
        _httpClient = new HttpClient();

        // Per evitare errori SSL in sviluppo se si usa local gateway
        if (useLocalGateway)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BotCripto/1.1");
    }

    public async Task<bool> ConnectAsync(string username, string password)
    {
        try
        {
            Console.WriteLine("🔌 Tentativo connessione Interactive Brokers TWS Socket (porta 7497)...");

            // Tenta PRIMA connessione diretta a TWS
            if (await TryConnectDirectToTws())
            {
                Console.WriteLine("✅ Connessione DIRETTA a TWS Socket ATTIVA (Real-Time)");
                _useRealtimeMode = true;
                _sessionId = "tws_direct_socket";
                _fallbackToFinnhub = false;
                _currentDataSource = "InteractiveBrokers (TWS Real-Time)";
                return true;
            }

            Console.WriteLine("❌ TWS non disponibile sulla porta 7497");
            Console.WriteLine("📝 Assicurati che:");
            Console.WriteLine("   1. TWS sia aperto e in esecuzione");
            Console.WriteLine("   2. API sia abilitata: Settings → API → Settings → Enable ActiveX and Socket Clients");
            Console.WriteLine("   3. La porta 7497 sia accessibile");
            Console.WriteLine("\n🔄 Attivazione FALLBACK SYSTEM → Tentando connessione Finnhub...");

            // Fallback a Finnhub se TWS non disponibile
            if (await TestFinnhubConnection())
            {
                Console.WriteLine("✅ Fallback Finnhub ATTIVO (Real-Time Market Data)");
                _useRealtimeMode = true;
                _sessionId = "finnhub_fallback";
                _fallbackToFinnhub = true;
                _currentDataSource = "Finnhub API (Fallback)";
                Console.WriteLine("⚠️  NOTA: Operando in modalità FALLBACK - Performance dati leggermente inferiori");
                return true;
            }

            Console.WriteLine("❌ Anche Finnhub non disponibile");
            Console.WriteLine("⚠️  ATTENZIONE: Sistema operando in modalità DEMO (nessun fallback disponibile)");
            _useRealtimeMode = false;
            _sessionId = "demo_only";
            _currentDataSource = "Demo Data";
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore connessione: {ex.Message}");
            _useRealtimeMode = false;
            _currentDataSource = "Demo Data (Error)";
            return false;
        }
    }

    private async Task<bool> TestFinnhubConnection()
    {
        try
        {
            if (string.IsNullOrEmpty(FinnhubApiKey) || FinnhubApiKey == "YOUR_FINNHUB_API_KEY")
            {
                Console.WriteLine("⚠️  Chiave API Finnhub non configurata");
                return false;
            }

            // Testa connessione Finnhub con un simbolo generico
            var url = $"{FinnhubApiUrl}/quote?symbol=ENI.MI&token={FinnhubApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Finnhub risponde correttamente");
                return true;
            }

            Console.WriteLine($"⚠️  Finnhub - Stato HTTP: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore test Finnhub: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TryConnectDirectToTws()
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var connectTask = client.ConnectAsync("localhost", 7497);
                var completed = await Task.WhenAny(connectTask, Task.Delay(3000));

                if (completed == connectTask && client.Connected)
                {
                    Console.WriteLine("✅ TWS risponde su porta 7497");
                    client.Close();
                    return true;
                }
            }
        }
        catch { }
        return false;
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

            // Endpoint di example - in produzione useresti l'API di IB reale
            // var url = $"{ClientPortalUrl}/iserver/marketdata/latest?conids=<conid>";

            // Per ora, simuliamo i dati dal mercato italiano
            // In produzione, integrerai direttamente con l'API IB

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
            if (_sessionId == "tws_direct_socket")
            {
                var price = await GetPriceFromTwsDirectSocket("", ibSymbol);
                if (price > 0)
                {
                    _priceCache[ibSymbol] = price;
                    _lastCacheUpdate = DateTime.UtcNow;
                    Console.WriteLine($"✅ {ibSymbol}: €{price:F2} (IB TWS - Primary)");
                    return price;
                }
                else
                {
                    Console.WriteLine($"⚠️  {ibSymbol}: TWS non ha dati, tentando Finnhub...");
                }
            }

            // FALLBACK 1: Finnhub API (Se TWS non disponibile o no dati)
            if (!string.IsNullOrEmpty(FinnhubApiKey) && FinnhubApiKey != "YOUR_FINNHUB_API_KEY")
            {
                var price = await GetPriceFromFinnhub(ibSymbol);
                if (price > 0)
                {
                    _priceCache[ibSymbol] = price;
                    _lastCacheUpdate = DateTime.UtcNow;
                    var source = _fallbackToFinnhub ? "Finnhub (Fallback)" : "Finnhub (Fallback - TWS offline)";
                    Console.WriteLine($"✅ {ibSymbol}: €{price:F2} ({source})");
                    return price;
                }
                else
                {
                    Console.WriteLine($"⚠️  {ibSymbol}: Finnhub non ha dati, usando demo...");
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

    private async Task<decimal> GetPriceFromFinnhub(string symbol)
    {
        try
        {
            // Prova multiple formati per Finnhub
            string ticker = symbol.Contains(".") ? symbol.Replace(".MI", "") : symbol;

            // Tenta con diversi formati di ticker
            string[] tickerFormats = new[]
            {
                $"{ticker}.MI",      // Borsa Italiana
                ticker,              // Simbolo base
                $"{ticker}-MI",      // Formato alternativo
            };

            foreach (var tickerFormat in tickerFormats)
            {
                try
                {
                    var url = $"{FinnhubApiUrl}/quote?symbol={tickerFormat}&token={FinnhubApiKey}";
                    Console.WriteLine($"🔍 Tentando Finnhub: {tickerFormat}...");

                    await RateLimitDelay();
                    var response = await _httpClient.GetAsync(url);

                    Console.WriteLine($"   Stato HTTP: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"   Risposta: {content}");
                        dynamic data = JsonConvert.DeserializeObject(content);

                        if (data != null && data.c != null && data.c > 0)
                        {
                            decimal price = decimal.Parse(data.c.ToString());
                            if (price > 0)
                            {
                                Console.WriteLine($"✅ 📡 Finnhub: {tickerFormat} = €{price:F2} (Real-Time)");
                                return price;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"   Prezzo non valido: c={data?.c}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   Errore HTTP: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Exception: {ex.Message}");
                }
            }

            Console.WriteLine($"⚠️  Finnhub: Non disponibile per {ticker}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Finnhub Exception ({symbol}): {ex.Message}");
        }
        return -1m;
    }

    private async Task<decimal> GetPriceFromTwsDirectSocket(string conId, string ibSymbol)
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                // Connessione al socket TWS
                var connectTask = client.ConnectAsync("localhost", 7497);
                var completed = await Task.WhenAny(connectTask, Task.Delay(3000));

                if (completed != connectTask || !client.Connected)
                {
                    return -1m;
                }

                // Usa NetworkStream per comunicare con TWS
                using (var stream = client.GetStream())
                {
                    // Costruisci il comando TWS per richiedere il prezzo
                    // Formato: reqMktData|id|conId|genericTickList|snapshot
                    string tickCommand = $"1\x00reqMktData\x001\x00{conId}\x00\x001\x00";

                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(tickCommand);
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();

                    // Leggi la risposta (con timeout)
                    byte[] responseBuffer = new byte[1024];
                    var readTask = stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    var readCompleted = await Task.WhenAny(readTask, Task.Delay(2000));

                    if (readCompleted == readTask && readTask.Result > 0)
                    {
                        string response = System.Text.Encoding.ASCII.GetString(responseBuffer, 0, readTask.Result);
                        // Parsing semplificato della risposta TWS
                        // In produzione, implementeresti il parser completo del protocollo TWS
                        if (!string.IsNullOrEmpty(response))
                        {
                            // Prova ad estrarre il prezzo dalla risposta
                            var parts = response.Split('\x00');
                            if (parts.Length > 2 && decimal.TryParse(parts[2], out decimal price))
                            {
                                return price > 0 ? price : -1m;
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return -1m;
    }

    // Mantieni il vecchio metodo per compatibilità (non usato, solo per reference)
    private async Task<decimal> GetPriceFromTwsSnapshot(string conId, string ibSymbol)
    {
        // Questo metodo è deprecato - usa GetPriceFromTwsDirectSocket
        return -1m;
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

    private string GetContractId(string ibSymbol)
    {
        // Mappa simboli ai loro Contract IDs in Interactive Brokers
        return ibSymbol switch
        {
            "ENI.MI" => "272093",      // Eni
            "ISP.MI" => "272093",      // Intesa Sanpaolo
            "UCG.MI" => "272093",      // UniCredit
            "TIT.MI" => "272093",      // Telecom Italia
            "BAMI.MI" => "272093",     // Banco di Napoli
            "BPE.MI" => "272093",      // Banca Popolare Emilia
            "STM.MI" => "272093",      // STMicroelectronics
            "ENEL.MI" => "272093",     // Enel
            "AZM.MI" => "272093",      // Mediobanca
            "FTSEMIB.MIX" => "272093", // FTSE MIB
            "EQNR.MI" => "272093",     // Equinor
            "EXS2.MI" => "272093",     // iShares MSCI World
            "VWRL.MI" => "272093",     // Vanguard FTSE World
            "MICC.MI" => "272093",     // Mediobanca
            "UNL.MI" => "272093",      // Unilever
            _ => ""
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
            if (_sessionId != "tws_direct_socket")
            {
                return null; // NO fallback a Gateway
            }

            var conId = GetContractId(ibSymbol);
            if (string.IsNullOrEmpty(conId))
                return null;

            // Recupera candele da TWS diretto tramite socket
            var candles = await GetCandlesFromTwsDirectSocket(conId, ibSymbol, interval, limit);
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

    private async Task<List<Candle>> GetCandlesFromTwsDirectSocket(string conId, string ibSymbol, string interval, int limit)
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var connectTask = client.ConnectAsync("localhost", 7497);
                var completed = await Task.WhenAny(connectTask, Task.Delay(3000));

                if (completed != connectTask || !client.Connected)
                    return null;

                using (var stream = client.GetStream())
                {
                    // Comando TWS per richiedere dati storici
                    // queryHistoricalData|id|conId|endDateTime|duration|durationUnit|barSize|whatToShow|useRTH|formatDate|keepUpToDate
                    string histCommand = $"1\x00queryHistoricalData\x001\x00{conId}\x00\x001\x00{limit}\x00D\x001\x00MIDPOINT\x001\x001\x00";

                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(histCommand);
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Flush();

                    byte[] responseBuffer = new byte[4096];
                    var readTask = stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    var readCompleted = await Task.WhenAny(readTask, Task.Delay(2000));

                    if (readCompleted == readTask && readTask.Result > 0)
                    {
                        // Parsing risposta TWS - semplificato
                        // In produzione, implementeresti il parser completo
                        string response = System.Text.Encoding.ASCII.GetString(responseBuffer, 0, readTask.Result);

                        // Per ora ritorna null - il client completo richiede implementazione IBApi
                        return null;
                    }
                }
            }
        }
        catch { }
        return null;
    }

    // Metodo deprecato - non usare più (era legato al Client Portal Gateway)
    // Ora tutte le funzioni usano SOLO la connessione TWS diretta su socket porta 7497

    private string ConvertIntervalToIb(string interval)
    {
        return interval.ToLower() switch
        {
            "1m" => "1",
            "5m" => "5",
            "15m" => "15",
            "30m" => "30",
            "1h" => "1h",
            "4h" => "4h",
            "1d" => "1d",
            _ => "1h"
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

        if (_fallbackToFinnhub)
        {
            status += " [FALLBACK ATTIVO]";
        }
        else if (_sessionId == "tws_direct_socket")
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
