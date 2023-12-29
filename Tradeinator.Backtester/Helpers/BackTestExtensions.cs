using System.Text;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

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

    public static BacktestTrade GetLastSpotTrade(this BacktestState s)
    {
        return s.GetAllSpotTrades().Last();
    }

    public static double LatestValue(this StockData data, string name)
    {
        return data.OutputValues[name].LastOrDefault();
    }

    /// <summary>
    /// Pretty prints the backtest results
    /// </summary>
    /// <param name="r">the backtest result</param>
    /// <param name="symbol">symbol the strategy was executed on</param>
    /// <param name="extraDetails">an extra details</param>
    public static void PrettyPrintResults(this BacktestResult r, string symbol, string extraDetails = "")
    {
        // copied from demo: https://github.com/NotCoffee418/SimpleBacktestLib/blob/main/SimpleBacktestLib.Demo/Program.cs
        // Formatting
        StringBuilder sb = new(Environment.NewLine);
        sb.AppendLine($"Backtest Results for {symbol}:");
        sb.AppendLine();
        Action<string, string> addLn = (string label, string value) =>
            sb.AppendLine((label + ':').PadRight(30) + value);

        // Add data we want
        addLn("Days Evaluated", r.EvaluatedCandleTimespan().TotalDays.ToString());
        addLn("First candle", r.FirstCandleTime.ToString());
        addLn("Last candle", r.LastCandleTime.ToString());
        sb.AppendLine();
        addLn("Trade Count", r.SpotTrades.Count.ToString());
        sb.AppendLine();
        addLn("P/L $", r.TotalProfitInQuote.ToString());
        addLn("Profit Strategy", $"{Math.Round(r.ProfitRatio * 100, 2)}%");
        addLn("Profit Buy & Hold", $"{Math.Round(r.BuyAndHoldProfitRatio * 100, 2)}%");
        sb.AppendLine();
        sb.AppendLine();
        addLn("starting base balance", r.StartingBaseBudget.ToString());
        addLn("starting quote balance", r.StartingQuoteBudget.ToString());
        sb.AppendLine();
        addLn("Final Balance Base", r.FinalBaseBudget.ToString());
        addLn("Final Balance Quote", r.FinalQuoteBudget.ToString());

        sb.AppendLine();

        addLn("Number of spot trades", r.SpotTrades.Count.ToString());
        addLn("Number of margin trades", r.MarginTrades.Count.ToString());

        sb.AppendLine();
        sb.AppendLine(extraDetails);
        // Print it
        Console.WriteLine(sb.ToString());
    }
}