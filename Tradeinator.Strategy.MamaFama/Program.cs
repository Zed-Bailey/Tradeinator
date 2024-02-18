using Serilog;
using Tradeinator.Configuration;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Strategy.MamaFama;
using Tradeinator.Strategy.Shared;


const string StrategySlug = "MAMAFAMA";


// load config
var configLoader = new ConfigurationLoader();


var exchangeHost = configLoader.Get("Rabbit:Host");
var exchangeName = configLoader.Get("Rabbit:Exchange");
var connectionString = configLoader.Get("ConnectionStrings:DbConnection");


if (ValidateNotNull(exchangeHost, exchangeName))
{
    Console.WriteLine("[ERROR] exchange host or name was null");
    return;
}

if (ValidateNotNull(connectionString))
{
    Console.WriteLine("[ERROR] db connection string was null");
    return;
}

await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("general.log")
    .CreateLogger();


var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancel requested");
    tokenSource.Cancel();
};

var barBindings = new[] { "bar.AUD/CHF" };

// setup exchange
using var exchange = new PublisherReceiverExchange(
    exchangeHost, exchangeName,
    barBindings
);

MamaFama defaultStrategy = new MamaFama
{
    UseSecondaryTrigger = false,
    StrategyVersion = "MamaFamaV1"
};

var strategies = new StrategyBuilder<MamaFama>(connectionString, logger, barBindings)
    .WithSlug(StrategySlug) // the slug of the strategy
    .WithExchange(exchange) // will register a listener to consume change events
    .WithMax(3) // max number of strategies that can be added
    .WithMessageNotificationCallback(OnSendMessageNotification) // register a callback for the send message notification event
    .WithDefaultStrategy(defaultStrategy) // register default strategy
    .Build();



await strategies.Init(); // will initalise all the strategies

logger.Information("Initialising strategies");
logger.Information("initialised");


logger.Information("Exchange starting");
await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;


void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}

bool ValidateNotNull(params string?[] values) => values.Any(string.IsNullOrEmpty);