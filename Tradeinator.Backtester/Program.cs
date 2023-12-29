using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Alpaca.Markets;
using CsvHelper;
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
// IBacktestRunner backtest = new DelayedMovingAverageCrossOver();
// IBacktestRunner backtest = new MMI();
// IBacktestRunner backtest = new FallingCrossMA();
// IBacktestRunner backtest = new HullMA();
IBacktestRunner backtest = new TrendTrading();

var metadata = GetMetaData(backtest);

if (metadata == null)
{
    throw new ArgumentNullException(nameof(metadata), "BackTestStrategyMetadata attribute was not found on the strategy");
}

var candleData = (await GetData(SYMBOL)).ToList();

// fetch meta data from the backtest metadata attribute



Console.WriteLine($"Running strategy: {metadata.StrategyName}");



// initialise the strategy
// this could do things like preload past data
await backtest.InitStrategy(SYMBOL, dataClient);


//
//
//
var builder = BacktestBuilder.CreateBuilder(candleData)
    .WithQuoteBudget((decimal) metadata.StartingBalance)
    .AddSpotFee(AmountType.Percentage, 0.15m, FeeSource.Base)
    .AddSpotFee(AmountType.Percentage, 0.15m, FeeSource.Quote)
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


BackTestStrategyMetadata? GetMetaData(IBacktestRunner b)
{
    return (BackTestStrategyMetadata?) Attribute.GetCustomAttribute(b.GetType(), typeof(BackTestStrategyMetadata));
}

async Task<IEnumerable<BacktestCandle>> GetData(string symbol)
{
    var dataPath = Path.Combine(Directory.GetCurrentDirectory(), $"{symbol.Replace("/", "_")}.csv");
    List<BacktestCandle> candleData;
    if (File.Exists(dataPath))
    {
        Console.WriteLine("existing data path found, loading from csv file");
        using (var reader = new StreamReader(dataPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            candleData = csv.GetRecords<BacktestCandle>().ToList();
        }
    }
    else
    {
        Console.WriteLine("No existing data path found, loading from api");
        candleData = (await FetchDataFromAlpaca(symbol, backtest.FromDate, backtest.ToDate, backtest.TimeFrame)).ToList();
        Console.WriteLine("Writing data to csv file");
        using (var writer = new StreamWriter(dataPath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(candleData);
        }
    }

    return candleData;
}

// load data
async Task<IEnumerable<BacktestCandle>> FetchDataFromAlpaca(string symbol, DateTime from, DateTime to, BarTimeFrame timeFrame)
{
    var page = await dataClient.ListHistoricalBarsAsync(
        new HistoricalCryptoBarsRequest(symbol, from, to, timeFrame));

    var bars = new List<IBar>(page.Items);
    var paginationToken = page.NextPageToken;
    while (paginationToken != null)
    {
        Console.WriteLine($"Getting next page of data. token={paginationToken}");
        var request = new HistoricalCryptoBarsRequest(symbol, from, to, timeFrame)
        {
            Pagination =
            {
                Token = paginationToken
            }
        };
        page = await dataClient.ListHistoricalBarsAsync(request);
        paginationToken = page.NextPageToken;
        bars.AddRange(page.Items);
    }

    return bars.Select(x => new BacktestCandle
    {
        Open = x.Open,
        High = x.High,
        Low = x.Low,
        Close = x.Close,
        Time = x.TimeUtc,
        Volume = x.Volume
    });
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
    sb.AppendLine(backtest.ExtraDetails());
    // Print it
    Console.WriteLine(sb.ToString());
}