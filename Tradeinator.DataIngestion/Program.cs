using Alpaca.Markets;
using DataIngestion;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.Shared;

// load the dotenv file into the environment
DotEnv.LoadEnvFiles(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// load the config
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

var host = config["Rabbit:Host"];
var exchangeName = config["Rabbit:Exchange"];

if (host is null || exchangeName is null)
{
    throw new ArgumentNullException(host ?? exchangeName);
}

using var exchange = new PublisherExchange(host, exchangeName);

// initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("data_ingestion.log")
    .CreateLogger();



//

using var client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));

var clock = await client.GetClockAsync();

Console.WriteLine(
    "Timestamp: {0}, NextOpen: {1}, NextClose: {2}",
    clock.TimestampUtc, clock.NextOpenUtc, clock.NextCloseUtc);


// using var dataStreaming = Environments.Paper.GetAlpacaDataStreamingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));
// dataStreaming.RegisterLoggers(logger);

using var dataStreaming = Environments.Paper.GetAlpacaCryptoStreamingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));
dataStreaming.RegisterLoggers(logger);

var authStatus = await dataStreaming.ConnectAndAuthenticateAsync();
if (authStatus == AuthStatus.Unauthorized || authStatus == AuthStatus.TooManyConnections)
{
    logger.Error("Failed to create a data streaming subscription, authStatus: {Status}", authStatus);
    return;
}
    


// watches the symbols file and will subscribe and unsubscribe from data streams when the file changes
await using var subscriptionManager = new SubscriptionManager(logger, exchange, dataStreaming, Directory.GetCurrentDirectory(), "symbols.txt");

logger.Information("Started watching symbols file: {Path}", subscriptionManager.SymbolsFile);

Console.WriteLine(">> Press any key to exit");
Console.ReadLine();


// var data = Environments.Paper.GetAlpacaDataClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));
//
// var startDate = new DateTime(2021, 01, 01);
// var endDate = new DateTime(2021, 2, 28);
// var page = await data.ListHistoricalBarsAsync(
//     new HistoricalBarsRequest("AAPL", startDate, endDate, BarTimeFrame.Day));
//
//
// while (true)
// {
//     foreach (var bar in page.Items)
//     {
//         exchange.Publish(bar, $"bar.{bar.Symbol}");
//         Thread.Sleep(2000);
//     }
//     Console.Write("re run: [y]/[n]: ");
//     if (Console.ReadLine() != "y") break;
// }