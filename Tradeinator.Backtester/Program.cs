using System.Diagnostics;
using System.Text;
using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Backtester.Strategies;
using Tradeinator.Shared;

var SYMBOL = "BTC/USD";

if (args.Length > 0)
{
    SYMBOL = args[0];
}

DotEnv.LoadEnvFiles(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// load the config
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

var key = config["ALPACA_KEY"];
var secret = config["ALPACA_SECRET"];

if (key == null || secret == null)
{
    throw new ArgumentNullException(key ?? secret);
}


var dataClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(key, secret));

// IBacktestRunner backtest = new MeanReversionBacktest();
IBacktestRunner backtest = new DelayedMovingAverageCrossOver();

// Create serilog logger with console and file sinks
// await using var logger = new LoggerConfiguration()
//     .WriteTo.Console()
//     .WriteTo.File(nameof(backtest) + ".log")
//     .CreateLogger();

var candleData = await GetData(SYMBOL, backtest.FromDate, backtest.ToDate);

// initialise the strategy
// this could do things like preload past data
await backtest.InitStrategy(SYMBOL, dataClient);

//
//
//
var builder = BacktestBuilder.CreateBuilder(candleData)
    .WithQuoteBudget(150)
    .WithBaseBudget(150)
    .WithDefaultMarginLongOrderSize(AmountType.Percentage, 10)
    .WithDefaultMarginShortOrderSize(AmountType.Percentage, 10)
    .OnTick(state =>
    {
        backtest.OnTick(state);
    })
    .OnLogEntry((entry, _) => Console.WriteLine(entry));
    

var stopwatch = new Stopwatch();
stopwatch.Start();
var results = await builder.RunAsync();
stopwatch.Stop();

// logger.Information("Completed backtest in {Time:00}ms", stopwatch.ElapsedMilliseconds);
Console.WriteLine($"Completed backtest in {stopwatch.ElapsedMilliseconds:00}ms");
DisplayBacktestResults(results);


// load data
async Task<List<BacktestCandle>> GetData(string symbol, DateTime from, DateTime to)
{
    var page = await dataClient.ListHistoricalBarsAsync(
        new HistoricalCryptoBarsRequest(symbol, from, to, BarTimeFrame.Hour));


    return page.Items.Select(x => new BacktestCandle
        {
            Open = x.Open,
            High = x.High,
            Low = x.Low,
            Close = x.Close,
            Time = x.TimeUtc,
            Volume = x.Volume
        })
        .ToList();
}

// prints the back test results
// copied from demo: https://github.com/NotCoffee418/SimpleBacktestLib/blob/main/SimpleBacktestLib.Demo/Program.cs
void DisplayBacktestResults(BacktestResult r)
{
    // Formatting
    StringBuilder sb = new(Environment.NewLine);
    sb.AppendLine($"Backtest Results for {SYMBOL}:");
    sb.AppendLine();
    Action<string, string> addLn = (string label, string value) =>
        sb.AppendLine((label + ':').PadRight(30) + value);

    // Add data we want
    addLn("Days Evaluated", r.EvaluatedCandleTimespan().TotalDays.ToString());
    addLn("Trade Count", r.SpotTrades.Count.ToString());
    sb.AppendLine();
    addLn("P/L $", r.TotalProfitInQuote.ToString());
    addLn("Profit Strategy", $"{Math.Round(r.ProfitRatio * 100, 2)}%");
    addLn("Profit Buy & Hold", $"{Math.Round(r.BuyAndHoldProfitRatio * 100, 2)}%");
    sb.AppendLine();
    addLn("Final Balance Base", r.FinalBaseBudget.ToString());
    addLn("Final Balance Quote", r.FinalQuoteBudget.ToString());


    // Print it
    Console.WriteLine(sb.ToString());
}