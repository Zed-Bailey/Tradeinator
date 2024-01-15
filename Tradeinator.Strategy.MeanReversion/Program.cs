using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.Shared;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.MeanReversion;
using Tradeinator.Strategy.Shared;

// implementation of the mean reversion strategy from
// https://github.com/alpacahq/alpaca-trade-api-csharp/blob/develop/UsageExamples/MeanReversionPaperOnly.cs

// load the dotenv file into the environment
DotEnv.LoadEnvFiles(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// load the config
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

var host = config["Rabbit:Host"] ?? throw new ArgumentNullException("RabbitmMQ host was null");
var exchangeName = config["Rabbit:Exchange"] ?? throw new ArgumentNullException("RabbitMQ exchange was null");

var symbols = new string[]
{
    "ETHUSD"
};


using var exchange = new ReceiverExchange(host, exchangeName, BindingGenerator.SymbolsToBarBindings(symbols));

var tokenSource = new CancellationTokenSource();

 // initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("mean_reversion.log")
    .CreateLogger();

logger.Information("Bound exchange to bindings: {Bindings}", exchange.Bindings);

// handle ctrl-c
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancellation requested");
    tokenSource.Cancel();
};

using var tradeManager = new AlpacaTradeManager(config["ALPACA_KEY"], config["ALPACA_SECRET"]);

using var strategy = new MeanReversion(tradeManager, config["ALPACA_KEY"], config["ALPACA_SECRET"], symbols);
await strategy.Init();

strategy.OnLogEntry += message => logger.Information("{Message}", message);




exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var bar = eventArgs.DeserializeToModel<Bar>();
    if (bar == null)
    {
        logger.Warning("Received bar was null after deserialization. body: {Body} | topic binding: {Binding}", eventArgs.BodyAsString(), eventArgs.RoutingKey);
        return;
    }
    Console.WriteLine(eventArgs.BodyAsString());
    strategy.NewBar(bar);
    
};


await exchange.StartConsuming(tokenSource.Token);