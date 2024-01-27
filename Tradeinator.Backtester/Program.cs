using System.Diagnostics;
using System.Reflection;
using System.Text;
using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleBacktestLib;
using SimpleBacktestLib.Models;
using Spectre.Console;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Configuration;
using Tradeinator.Shared;

// default symbol strategies are executed on
var SYMBOL = "BTC/USD";
var runFancyBacktest = false;
if (args.Length > 0)
{
    SYMBOL = args[0];
    if (args.Contains("--fancy"))
    {
        runFancyBacktest = true;
    }
}

// required otherwise the console will be all messed up
Console.CancelKeyPress += (sender, eventArgs) => AnsiConsole.WriteLine();

var stopwatch = new Stopwatch();

var config = new ConfigurationLoader();

var key = config["ALPACA_KEY"];
var secret = config["ALPACA_SECRET"];

if (key == null || secret == null)
{
    throw new ArgumentNullException(key ?? secret);
}

// clear console so it's nice and clean
AnsiConsole.Clear();





var availableStrategies = GetAvailableStrategies();
var strategy = GetStrategyToRun(availableStrategies);
if (strategy == null)
{
    AnsiConsole.MarkupLine("[red]Backtest not selected[/]");
    return;
}

AnsiConsole.MarkupLineInterpolated($"Running Backtest for strategy: [lime]{strategy.attribute.StrategyName}[/]");

var backtest = strategy.backtestRunner;


var dataClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(key, secret));

stopwatch.Start();
var candleData = (await DataFetcher.GetData(SYMBOL, Directory.GetCurrentDirectory(), backtest.FromDate, backtest.ToDate, backtest.TimeFrame, dataClient)).ToList();
stopwatch.Stop();
if (candleData.Count == 0)
{
    AnsiConsole.MarkupLine("[red]Failed to load data, there was no data[/]");
    return;
}

AnsiConsole.MarkupLineInterpolated($"[green]Loaded data in {stopwatch.ElapsedMilliseconds:F2}ms[/]");



// initialise the strategy
// this could do things like preload past data
await backtest.InitStrategy(SYMBOL, dataClient);


//
//
var builder = BacktestBuilder.CreateBuilder(candleData)
    .WithQuoteBudget((decimal) strategy.attribute.StartingBalance / 2)
    .WithBaseBudget((decimal) strategy.attribute.StartingBalance / 2)
    .WithMarginLeverageRatio(30)
    .WithDefaultMarginLongOrderSize(AmountType.Percentage, 5)
    .AddSpotFee(AmountType.Percentage, 0.15m, FeeSource.Base)
    .AddSpotFee(AmountType.Percentage, 0.15m, FeeSource.Quote)
    .EvaluateBetween(backtest.FromDate, backtest.ToDate);


// index of the last candle
var lastCandleIndex = candleData.FindIndex(c => c.Time > backtest.ToDate) - 1;

BacktestResult? backtestResult = null;

stopwatch.Restart();

if (runFancyBacktest)
    backtestResult = await FancyBackTest.RunFancyBacktest(builder, backtest, lastCandleIndex);
else
    backtestResult = await SimpleBackTest.RunSimpleBacktest(builder, backtest, lastCandleIndex);

stopwatch.Stop();

AnsiConsole.MarkupLineInterpolated($"[green]Completed backtest in {stopwatch.ElapsedMilliseconds:00}ms [/]");

backtestResult.PrettyPrintResults(SYMBOL, backtest.ExtraDetails());
return;


// gets all types from the assembly with the backtest meta data attribute
List<AvailableStrategy> GetAvailableStrategies()
{
    var list = new List<AvailableStrategy>();

    // get all types in te current running assembly
    var types = Assembly.GetExecutingAssembly().GetTypes();

    foreach (var type in types)
    {
        // get the attribute
        var attrib = (BackTestStrategyMetadata?) Attribute.GetCustomAttribute(type, typeof(BackTestStrategyMetadata));
        // check that the type can be assigned to the abstract class
        var assignable = type.IsAssignableTo(typeof(BacktestRunner));
        if (attrib != null && assignable)
        {

            BacktestRunner? b = (BacktestRunner?) Activator.CreateInstance(type);
            if (b != null)
            {
                list.Add(new AvailableStrategy(b, attrib));
            }
            else
            {
                Console.WriteLine($"Failed to get strategy implementation for: {attrib.StrategyName}");
            }
        }
    }

    return list;
}

AvailableStrategy? GetStrategyToRun(List<AvailableStrategy> strategies)
{
    AnsiConsole.Write("");
    // display selection prompt
    var backtestSelected = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select a strategy to run")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more strategies)[/]")
            .AddChoices(strategies.Select(x => x.attribute.StrategyName))
    );
    
    return  availableStrategies.FirstOrDefault(x => x.attribute.StrategyName == backtestSelected);
}