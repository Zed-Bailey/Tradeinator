using System.Text;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public static class BackTestExtensions
{
    /// <summary>
    /// Converts a BackTestCandle to a TickerData object used by the indicators library
    /// </summary>
    /// <param name="c">candle</param>
    /// <returns>converted ticker data object</returns>
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
        addLn("Trades per day", $"{r.SpotTrades.Count / r.EvaluatedCandleTimespan().TotalDays:F}");
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

        addLn("% profitable trades", CalculatePctProfitable(r.SpotTrades.ToList()).ToString());
        
        sb.AppendLine();
        sb.AppendLine(extraDetails);
        
        // Print it
        Console.WriteLine(sb.ToString());
    }

    /// <summary>
    /// calculates the win loss ratio of the trades the system made
    /// assumes that their is no pyramiding (only 1 trade is open at a time)
    /// </summary>
    /// <param name="trades"></param>
    /// <returns></returns>
    private static double CalculatePctProfitable(List<BacktestTrade> trades)
    {

        double wins = 0;
        double closed = 0;
        
        if (trades.Count == 0) return 0;
        
        for (int i = 1; i < trades.Count; i++)
        {
            var trade = trades[i];
            if (trade.Action == TradeOperation.Sell)
            {
                var previous = trades[i - 1];
                // check we made a profit on the sell price - buy price
                if (trade.QuotePrice - previous.QuotePrice > 0)
                    wins++;
                closed++;
            }
        }

        return (wins / closed)*100;
    }
}