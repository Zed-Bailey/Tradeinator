using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.DataIngestion.Forex;
using Tradeinator.DataIngestion.Shared;
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
    // .WriteTo.File("data_ingestion.log")
    .CreateLogger();



//
var subscriptionManager = new ForexSubscriptionManager(logger, Directory.GetCurrentDirectory(), "symbols.txt");
var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Ctrl+C pressed shutting down");
    tokenSource.Cancel();
};

var apiToken = config["OANDA_API_TOKEN"];
var oandaConnection = new OandaConnection(apiToken);

// timer runs every 30 minutes
await using var timer = new Timer(TimerTrigger, null, TimeSpan.Zero, new TimeSpan(0,30, 0));


await Task.Delay(-1, tokenSource.Token);


return;

async void TimerTrigger(object? state)
{

    foreach (var symbol in subscriptionManager.Symbols)
    { 
        var bar = await oandaConnection.GetLatestData("AUD_CHF");
        if (bar is null)
        {
            logger.Warning("Received bar for {Symbol} was null", symbol);
            continue;
        }
        
        // post bar to exchange
        exchange.Publish(bar, $"bar.{symbol}");
        logger.Information("Received bar for {Symbol} | {Date} | {O} {H} {L} {C}", 
            symbol, bar.TimeUtc, bar.Open, bar.High, bar.Low, bar.Close)
        ;
        
    }
   
}


