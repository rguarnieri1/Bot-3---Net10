using BotCripto.Models;
using BotCripto.Strategies;

namespace BotCripto.Services;

public class BotSchedulerService
{
    private readonly InteractiveBrokersDataService _dataService;
    private readonly NotificationService _notificationService;
    private readonly ReportingService _reportingService;
    private readonly EmaRibbonTrendFollowingStrategy _emaRibbonStrategy;
    private readonly RiskManager _riskManager;
    private Timer? _marketCheckTimer;
    private Timer? _weeklyReportTimer;
    private bool _isRunning = false;

    private decimal _currentAccountValue = 1000m;
    private int _checksPerformed = 0;
    private int _signalsGenerated = 0;
    private int _tradesRecorded = 0;

    public BotSchedulerService(decimal initialCapital = 1000m)
    {
        _dataService = new InteractiveBrokersDataService();
        _notificationService = new NotificationService();
        _reportingService = new ReportingService();
        _emaRibbonStrategy = new EmaRibbonTrendFollowingStrategy();
        _riskManager = new RiskManager(
            initialCapital,
            riskPercentPerTrade: 0.02m,      // 2% rischio per trade
            rewardRiskRatio: 2.0m,            // 2:1 R:R
            maxPositionSizePercent: 0.10m,    // Max 10% per trade
            maxLeverage: 2.0m,                // Max 2.0x leva
            commissionsPercent: 0.10m,        // 0.1% commissioni
            taxRate: 0.26m                    // 26% tasse
        );

        _currentAccountValue = initialCapital;
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            Console.WriteLine("Bot è già in esecuzione.");
            return;
        }

        _isRunning = true;
        Console.WriteLine("\n🤖 Bot Mercato Italiano - ERTF Ita avviato!");
        Console.WriteLine("📊 Fonte Dati: Interactive Brokers (Mercato Italiano)");
        Console.WriteLine("📊 Monitoraggio ogni 15 minuti...\n");

        // Connetti a Interactive Brokers
        Console.WriteLine("🔌 Tentativo connessione Interactive Brokers...");
        bool connected = await _dataService.ConnectAsync("", "");
        if (!connected)
        {
            Console.WriteLine("\n⚠️  AVVISO: Connessione IB non disponibile");
            Console.WriteLine("Continuerò con dati demo per il testing\n");
        }
        else
        {
            Console.WriteLine("✅ Connesso a Interactive Brokers!\n");
        }

        // Esegui il primo controllo immediatamente
        await CheckMarketAsync();

        // Timer per il controllo ogni 15 minuti (900000 ms)
        _marketCheckTimer = new Timer(
            async _ => await CheckMarketAsync(),
            null,
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(15));

        // Timer per il report settimanale (ogni lunedì alle 00:00)
        var now = DateTime.UtcNow;
        var nextMonday = now.AddDays((DayOfWeek.Monday - now.DayOfWeek + 7) % 7);
        if (nextMonday <= now)
            nextMonday = nextMonday.AddDays(7);

        var timeUntilMonday = nextMonday - now;
        _weeklyReportTimer = new Timer(
            _ => _reportingService.GenerateWeeklyReport(),
            null,
            timeUntilMonday,
            TimeSpan.FromDays(7));

        Console.WriteLine($"⏰ Prossimo report settimanale: {nextMonday:yyyy-MM-dd HH:mm:ss}");

        await Task.Delay(-1);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _marketCheckTimer?.Dispose();
        _weeklyReportTimer?.Dispose();
        Console.WriteLine("\n🛑 Bot Cripto fermato.");
    }

    private async Task CheckMarketAsync()
    {
        try
        {
            _checksPerformed++;
            Console.WriteLine($"\n⏱️  Ciclo #{_checksPerformed} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // Recupera azioni dal mercato italiano
            var italianStocks = await _dataService.GetItalianStocksAsync();
            Console.WriteLine($"📈 Analizzando {italianStocks.Count} titoli mercato italiano...");

            int signalsFound = 0;
            int signalsFiltered = 0;
            var openTrades = _reportingService.GetAllTrades().Where(t => t.Status == "Open").ToList();

            foreach (var stock in italianStocks)
            {
                try
                {
                    // Recupera candele per l'analisi
                    var candles = await _dataService.GetCandlesAsync(stock.Symbol, "4h", 100);

                    if (candles.Count < 50)
                        continue;

                    var volatilityPercent = CalculateVolatility(candles);

                    // ✅ STRATEGIA PRINCIPALE: EMA RIBBON TREND FOLLOWING

                    // 1️⃣ EMA Ribbon Trend Following + Candle Confirmation (Principale - Win Rate 60-62%)
                    var emaRibbonResult = _emaRibbonStrategy.Analyze(stock.Symbol, candles);

                    if (emaRibbonResult.IsSignal)
                    {
                        // Valida con RiskManager
                        var positionResult = _riskManager.CalculatePosition(
                            stock.Symbol,
                            stock.CurrentPrice,
                            volatilityPercent,
                            openTrades,
                            _currentAccountValue
                        );

                        if (positionResult.IsValid)
                        {
                            emaRibbonResult.Indicators["PositionSize"] = positionResult.PositionSize;
                            emaRibbonResult.Indicators["RiskRewardRatio"] = positionResult.RiskRewardRatio;
                            emaRibbonResult.Indicators["Leverage"] = positionResult.LeverageRatio;
                            emaRibbonResult.Indicators["ExpectedProfit"] = positionResult.ExpectedProfit;

                            await _notificationService.SendNotificationAsync(emaRibbonResult);
                            _reportingService.RecordTrade(
                                stock.Symbol,
                                stock.CurrentPrice,
                                "EMA Ribbon Trend Following (Primary)");

                            _signalsGenerated++;
                            _tradesRecorded++;
                            signalsFound++;
                        }
                        else
                        {
                            signalsFiltered++;
                        }
                    }

                    await Task.Delay(50);  // Delay più breve
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  {stock.Symbol}: {ex.Message}");
                }
            }

            // Report del ciclo
            Console.WriteLine($"\n✅ Ciclo completato:");
            Console.WriteLine($"   • Segnali trovati: {signalsFound}");
            Console.WriteLine($"   • Segnali filtrati (rischio): {signalsFiltered}");
            Console.WriteLine($"   • Trade aperti: {openTrades.Count}");
            Console.WriteLine($"   • Account value (stimato): €{_currentAccountValue:F2}");

            // Calcola e mostra metriche giornaliere ogni 10 cicli
            if (_checksPerformed % 10 == 0)
            {
                var allTrades = _reportingService.GetAllTrades();
                var metrics = _riskManager.CalculatePerformanceMetrics(allTrades, 1000m);
                if (metrics.TotalTrades > 0)
                {
                    Console.WriteLine($"\n📊 Metriche cumulative (dopo {_checksPerformed} cicli):");
                    Console.WriteLine($"   • Win Rate: {metrics.WinRate:P}");
                    Console.WriteLine($"   • Profit Factor: {metrics.ProfitFactor:F2}");
                    Console.WriteLine($"   • Total P&L: €{metrics.TotalProfit:F2}");
                    Console.WriteLine($"   • Expectancy: €{metrics.Expectancy:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Errore nel controllo del mercato: {ex.Message}");
        }
    }

    private decimal CalculateVolatility(List<Candle> candles)
    {
        if (candles.Count < 20)
            return 0.02m;

        var closes = candles.TakeLast(20).Select(c => c.Close).ToList();
        var returns = new List<decimal>();

        for (int i = 1; i < closes.Count; i++)
        {
            returns.Add((closes[i] - closes[i - 1]) / closes[i - 1]);
        }

        var avg = returns.Average();
        var variance = returns.Sum(r => (r - avg) * (r - avg)) / returns.Count;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return Math.Max(0.01m, Math.Min(stdDev, 0.08m));
    }
}
