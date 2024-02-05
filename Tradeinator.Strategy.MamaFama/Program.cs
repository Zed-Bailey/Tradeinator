// See https://aka.ms/new-console-template for more information

using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using Serilog;
using Tradeinator.Configuration;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.MamaFama;
using Tradeinator.Strategy.Shared;

// load config
var configLoader = new ConfigurationLoader();

var strategyVersion1 = configLoader.Get("MamaFama:Accounts:SV1");
var strategyVersion2 = configLoader.Get("MamaFama:Accounts:SV2");
var strategyVersion3 = configLoader.Get("MamaFama:Accounts:SV3");
var apiToken = configLoader.Get("OANDA_API_TOKEN");


if (string.IsNullOrEmpty(strategyVersion1) || string.IsNullOrEmpty(strategyVersion2) || string.IsNullOrEmpty(strategyVersion3))
{
    Console.WriteLine("[ERROR] empty account number(s)");
    return;
}

if (string.IsNullOrEmpty(apiToken))
{
    Console.WriteLine("[ERROR] Oanda api token was null or empty");
    return;
}

var exchangeHost = configLoader.Get("Rabbit:Host");
var exchangeName = configLoader.Get("Rabbit:Exchange");

if (string.IsNullOrEmpty(exchangeHost) || string.IsNullOrEmpty(exchangeName))
{
    Console.WriteLine("[ERROR] exchange host or name was null");
    return;
}
// initialise serilog loggers for each strategy, writing to console and file

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



await using var strategy1 = new MamaFama(strategyVersion1, apiToken, "MamaFamaV1")
{
    UseSecondaryTrigger = false,
};
await using var strategy2 = new MamaFama(strategyVersion2, apiToken, "MamaFamaV2")
{
    RrsiLevel = 50,
    UseSecondaryTrigger = true,
};

await using var strategy3 = new MamaFama(strategyVersion3, apiToken, "MamaFamaV3")
{
    RrsiLevel = 45,
    UseSecondaryTrigger = true,
};


strategy1.SendMessageNotification += OnSendMessageNotification;
strategy2.SendMessageNotification += OnSendMessageNotification;
strategy3.SendMessageNotification += OnSendMessageNotification;

logger.Information("Initialising strategies");
await strategy1.Init();
await strategy2.Init();
await strategy3.Init();
logger.Information("initialised");

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var bar = eventArgs.DeserializeToModel<Bar>();
    logger.Information("received new bar {Bar}", bar);
    if (bar == null)
    {
        logger.Warning("Received bar was null after deserialization. body: {Body} | topic binding: {Binding}", eventArgs.BodyAsString(), eventArgs.RoutingKey);
        return;
    }
    
    strategy1.NewBar(bar);
    strategy2.NewBar(bar);
    strategy3.NewBar(bar);
};

exchange.Publish(new SystemMessageEventArgs(new SystemMessage()
{
    Message = "Initialised MamaFama strategy",
    Priority = MessagePriority.Information,
    StrategyName = "MamaFama",
    Symbol = "AUD/CHF"
}), "notifications.AUD/CHF");


logger.Information("Exchange starting");
await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;



void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}
