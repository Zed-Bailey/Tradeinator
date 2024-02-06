using RabbitMQ.Client.Events;
using Serilog;
using Tradeinator.Configuration;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.MamaFama;
using Tradeinator.Strategy.Shared;


const string StrategySlug = "MAMAFAMA";


// load config
var configLoader = new ConfigurationLoader();

var strategyVersion1 = configLoader.Get("MamaFama:Accounts:SV1");
var strategyVersion2 = configLoader.Get("MamaFama:Accounts:SV2");
var strategyVersion3 = configLoader.Get("MamaFama:Accounts:SV3");
var apiToken = configLoader.Get("OANDA_API_TOKEN");
var exchangeHost = configLoader.Get("Rabbit:Host");
var exchangeName = configLoader.Get("Rabbit:Exchange");
var connectionString = configLoader.Get("ConnectionStrings:DbConnection");

if (ValidateNotNull(strategyVersion1, strategyVersion2, strategyVersion3))
{
    Console.WriteLine("[ERROR] empty account number(s)");
    return;
}

if (ValidateNotNull(apiToken))
{
    Console.WriteLine("[ERROR] Oanda api token was null or empty");
    return;
}

if (ValidateNotNull(exchangeHost, exchangeName))
{
    Console.WriteLine("[ERROR] exchange host or name was null");
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


// setup exchange
using var exchange = new PublisherReceiverExchange(
    exchangeHost, exchangeName,
    "bar.AUD/CHF"
);


/*
 strategies = Load
 .fromDB // json configs exist in db
 .elseCreate // else load strategy with default values
 
 ....
 ....
 
 // dispose strategies
 await strategies.foreachAsync(disposeasync);
 
 */


var strategies = new StrategyBuilder<MamaFama>(connectionString)
    .WithSlug(StrategySlug) // the slug of the strategy
    .WithExchange(exchange) // will register a listener to consume change events
    .WithMax(3) // max number of strategies that can be added
    .WithMessageNotificationCallback(OnSendMessageNotification) // register a callback for the send message notification event
    .LoadFromDb() // load strategies from db
    .ElseCreate(new MamaFama()) // create default strategy, serialise it and save it to DB
    .Build();

foreach (var strategy in strategies.LoadedStrategies)
{
    Console.WriteLine(strategy.RrsiLevel);
}
return;

await strategies.Init(); // will initalise all the strategies






// await using var strategy1 = new MamaFama(strategyVersion1, apiToken, "MamaFamaV1")
// {
//     UseSecondaryTrigger = false,
// };
// await using var strategy2 = new MamaFama(strategyVersion2, apiToken, "MamaFamaV2")
// {
//     RrsiLevel = 50,
//     UseSecondaryTrigger = true,
// };
//
// await using var strategy3 = new MamaFama(strategyVersion3, apiToken, "MamaFamaV3")
// {
//     RrsiLevel = 45,
//     UseSecondaryTrigger = true,
// };
//
//
// strategy1.SendMessageNotification += OnSendMessageNotification;
// strategy2.SendMessageNotification += OnSendMessageNotification;
// strategy3.SendMessageNotification += OnSendMessageNotification;

logger.Information("Initialising strategies");
// await strategy1.Init();
// await strategy2.Init();
// await strategy3.Init();
logger.Information("initialised");

exchange.ConsumerOnReceive += NewBarReceive;

logger.Information("Exchange starting");
await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;


void NewBarReceive(object? sender, BasicDeliverEventArgs eventArgs)
{
    var bar = eventArgs.DeserializeToModel<Bar>();
    logger.Information("received new bar {Bar}", bar);
    if (bar == null)
    {
        logger.Warning("Received bar was null after deserialization. body: {Body} | topic binding: {Binding}", eventArgs.BodyAsString(), eventArgs.RoutingKey);
        return;
    }
    //
    // strategy1.NewBar(bar);
    // strategy2.NewBar(bar);
    // strategy3.NewBar(bar);
}

void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}

bool ValidateNotNull(params string?[] values) => values.Any(string.IsNullOrEmpty);