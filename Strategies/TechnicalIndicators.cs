using BotCripto.Models;

namespace BotCripto.Strategies;

public static class TechnicalIndicators
{
    public static List<decimal> CalculateRSI(List<decimal> prices, int period = 14)
    {
        var rsi = new List<decimal>();
        if (prices.Count < period + 1)
            return rsi;

        var deltas = new List<decimal>();
        for (int i = 1; i < prices.Count; i++)
            deltas.Add(prices[i] - prices[i - 1]);

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 0; i < period; i++)
        {
            gains.Add(deltas[i] > 0 ? deltas[i] : 0);
            losses.Add(deltas[i] < 0 ? Math.Abs(deltas[i]) : 0);
        }

        var avgGain = gains.Sum() / period;
        var avgLoss = losses.Sum() / period;

        for (int i = period; i < deltas.Count; i++)
        {
            avgGain = ((avgGain * (period - 1)) + (deltas[i] > 0 ? deltas[i] : 0)) / period;
            avgLoss = ((avgLoss * (period - 1)) + (deltas[i] < 0 ? Math.Abs(deltas[i]) : 0)) / period;

            var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            var rsiValue = 100 - (100 / (1 + rs));
            rsi.Add(rsiValue);
        }

        return rsi;
    }

    public static (List<decimal> macd, List<decimal> signal, List<decimal> histogram) CalculateMACD(
        List<decimal> prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var ema12 = CalculateEMA(prices, fastPeriod);
        var ema26 = CalculateEMA(prices, slowPeriod);

        var macd = new List<decimal>();
        var minLength = Math.Min(ema12.Count, ema26.Count);
        for (int i = 0; i < minLength; i++)
            macd.Add(ema12[ema12.Count - minLength + i] - ema26[ema26.Count - minLength + i]);

        var signal = CalculateEMA(macd.Cast<decimal>().ToList(), signalPeriod);
        var histogram = new List<decimal>();

        var histLength = Math.Min(macd.Count, signal.Count);
        for (int i = 0; i < histLength; i++)
            histogram.Add(macd[macd.Count - histLength + i] - signal[signal.Count - histLength + i]);

        return (macd, signal, histogram);
    }

    public static List<decimal> CalculateEMA(List<decimal> prices, int period)
    {
        var ema = new List<decimal>();
        if (prices.Count < period)
            return ema;

        var multiplier = 2m / (period + 1);
        var sma = prices.Take(period).Sum() / period;
        ema.Add(sma);

        for (int i = period; i < prices.Count; i++)
        {
            var emaValue = (prices[i] - ema.Last()) * multiplier + ema.Last();
            ema.Add(emaValue);
        }

        return ema;
    }

    public static List<decimal> CalculateSMA(List<decimal> prices, int period)
    {
        var sma = new List<decimal>();
        if (prices.Count < period)
            return sma;

        for (int i = period - 1; i < prices.Count; i++)
        {
            var average = prices.Skip(i - period + 1).Take(period).Average();
            sma.Add(average);
        }

        return sma;
    }

    public static (List<decimal> upper, List<decimal> middle, List<decimal> lower) CalculateBollingerBands(
        List<decimal> prices, int period = 20, decimal stdDevMultiplier = 2)
    {
        var upper = new List<decimal>();
        var middle = new List<decimal>();
        var lower = new List<decimal>();

        if (prices.Count < period)
            return (upper, middle, lower);

        for (int i = period - 1; i < prices.Count; i++)
        {
            var subset = prices.Skip(i - period + 1).Take(period).ToList();
            var sma = subset.Average();
            var stdDev = (decimal)Math.Sqrt((double)subset.Sum(x => (x - (decimal)sma) * (x - (decimal)sma)) / period);

            middle.Add(sma);
            upper.Add(sma + (stdDev * stdDevMultiplier));
            lower.Add(sma - (stdDev * stdDevMultiplier));
        }

        return (upper, middle, lower);
    }
}
