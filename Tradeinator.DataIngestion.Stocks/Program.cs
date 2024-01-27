using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.Configuration;
using Tradeinator.DataIngestion.Shared;
using Tradeinator.Shared;


var config = new ConfigurationLoader();

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



using var dataStreaming = Environments.Paper.GetAlpacaDataStreamingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));
dataStreaming.RegisterLoggers(logger);

var authStatus = await dataStreaming.ConnectAndAuthenticateAsync();
if (authStatus == AuthStatus.Unauthorized || authStatus == AuthStatus.TooManyConnections)
{
    logger.Error("Failed to create a data streaming subscription, authStatus: {Status}", authStatus);
    return;
}
    


// watches the symbols file and will subscribe and unsubscribe from data streams when the file changes
await using var subscriptionManager = new StockSubscriptionManager(logger, exchange, dataStreaming, Directory.GetCurrentDirectory(), "symbols.txt");

logger.Information("Started watching symbols file: {Path}", subscriptionManager.SymbolsFile);

var tokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancellation Requested");
    subscriptionManager.UnsubscribeFromAll().Wait();
    tokenSource.Cancel();
};

await Task.Delay(-1, tokenSource.Token);


