using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;

namespace Tradeinator.Backtester;

public static class BackTestExtensions
{
    public static TickerData CandleToTicker(this BacktestCandle c)
    {
        return new TickerData
        {
            Open = (double) c.Open,
            High = (double) c.High,
            Low = (double) c.Low,
            Close = (double) c.Close,
            Volume = (double) c.Volume,
            Date = c.Time
        };
    }

    public static double LatestValue(this StockData data, string name)
    {
        return data.OutputValues[name].LastOrDefault();
    }
}