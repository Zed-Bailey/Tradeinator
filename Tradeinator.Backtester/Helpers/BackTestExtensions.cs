using System.Text;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using SimpleBacktestLib.Models;

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
        
        sb.AppendLine();
        
        var marginAnalysis = MarginTradeAnalysis(r.MarginTrades.ToList());
        if (!string.IsNullOrWhiteSpace(marginAnalysis))
            sb.AppendLine(marginAnalysis);
        
        sb.AppendLine();
        
        sb.AppendLine(extraDetails);
        
        // Print it
        Console.WriteLine(sb.ToString());
    }


    private static string MarginTradeAnalysis(List<MarginPosition> positions)
    {
        if (!positions.Any()) return "";

        var winning = 0.0;
        var loosing = 0.0;
        var avgProfitPerTrade = 0m;
        var avgNumBarsOpen = 0;
        
        foreach (var pos in positions)
        {
            var profit = pos.MarginDirection == TradeType.MarginLong ? pos.BaseProfit : pos.QuoteProfit;
                
            if (profit > 0.0m)
            {
                winning++;
                avgProfitPerTrade += profit;
            }
            else loosing++;

            avgNumBarsOpen += pos.CandleCloseIndex - pos.CandleOpenIndex;
        }


        avgProfitPerTrade = avgProfitPerTrade / (decimal) winning;
        // avgNumBarsOpen = avgNumBarsOpen / positions.Count;
        
        return $"""
               win: {winning}
               loose : {loosing}
               win % : {(winning / (winning + loosing))*100:F}
               
               avg profit per trade : $ {avgProfitPerTrade:F3}
               
               avg num bars trade is open : {avgNumBarsOpen}
               avg num bars trade is open : {avgNumBarsOpen/positions.Count}
               """;
    }
    
 
}