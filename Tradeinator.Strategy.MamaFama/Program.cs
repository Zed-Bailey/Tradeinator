// See https://aka.ms/new-console-template for more information

using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using Serilog;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.MamaFama;
using Tradeinator.Strategy.Shared;

// load config
var configLoader = new ConfigurationLoader(AppContext.BaseDirectory);
configLoader.LoadConfiguration();

var strategyVersion1 = configLoader.Get("Accounts:SV1");
var strategyVersion2 = configLoader.Get("Accounts:SV2");
var strategyVersion3 = configLoader.Get("Accounts:SV3");
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
    configLoader.Get("rabbit:Host"), configLoader.Get("Rabbit:Name"),
    "AUD/CHF"
);



await using var strategy1 = new MamaFama(strategyVersion1, apiToken)
{
    UseSecondaryTrigger = false,
    StrategyVersion = "MamaFamaV1"
};
await using var strategy2 = new MamaFama(strategyVersion2, apiToken)
{
    RrsiLevel = 50,
    UseSecondaryTrigger = true,
    StrategyVersion = "MamaFamaV2"
};

await using var strategy3 = new MamaFama(strategyVersion3, apiToken)
{
    RrsiLevel = 45,
    UseSecondaryTrigger = true,
    StrategyVersion = "MamaFamaV3"
};


strategy1.SendMessageNotification += OnSendMessageNotification;
strategy2.SendMessageNotification += OnSendMessageNotification;
strategy3.SendMessageNotification += OnSendMessageNotification;

await strategy1.Init();
await strategy2.Init();
await strategy3.Init();

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var bar = eventArgs.DeserializeToModel<Bar>();
    if (bar == null)
    {
        logger.Warning("Received bar was null after deserialization. body: {Body} | topic binding: {Binding}", eventArgs.BodyAsString(), eventArgs.RoutingKey);
        return;
    }
    
    strategy1.NewBar(bar);
    strategy2.NewBar(bar);
    strategy3.NewBar(bar);
};

await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;



void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}