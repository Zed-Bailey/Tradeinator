using System.Diagnostics;
using System.Reflection;
using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using SimpleBacktestLib;
using Spectre.Console;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Shared;

// default symbol strategies are executed on
var SYMBOL = "BTC/USD";

if (args.Length > 0)
{
    SYMBOL = args[0];
}

// required otherwise the console will be all messed up
Console.CancelKeyPress += (sender, eventArgs) => AnsiConsole.WriteLine();


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
var candleData = (await DataFetcher.GetData(SYMBOL, Directory.GetCurrentDirectory(), backtest.FromDate, backtest.ToDate, backtest.TimeFrame, dataClient)).ToList();




// initialise the strategy
// this could do things like preload past data
await backtest.InitStrategy(SYMBOL, dataClient);


//
//
var builder = BacktestBuilder.CreateBuilder(candleData)
    .WithQuoteBudget((decimal) strategy.attribute.StartingBalance)
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

Console.WriteLine($"Completed backtest in {stopwatch.ElapsedMilliseconds:00}ms");
results.PrettyPrintResults(SYMBOL, backtest.ExtraDetails());
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
        var assignable = type.IsAssignableTo(typeof(IBacktestRunner));
        if (attrib != null && assignable)
        {

            IBacktestRunner? b = (IBacktestRunner?) Activator.CreateInstance(type);
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