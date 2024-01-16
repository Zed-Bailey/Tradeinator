// See https://aka.ms/new-console-template for more information

using Serilog;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.MamaFama;
using Tradeinator.Strategy.Shared;

// load config
var configLoader = new ConfigurationLoader(Directory.GetCurrentDirectory());
configLoader.LoadConfiguration();

var strategyVersion1 = configLoader.Get("Account:SV1");
var strategyVersion2 = configLoader.Get("Account:SV2");
var apiToken = configLoader.Get("OANDA_API_TOKEN");



// initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("mama_fama.log")
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



using var strategy1 = new MamaFamaV1(strategyVersion1, apiToken);
// var strategy2 = new MamaFamaV2(strategyVersion2, apiToken);


strategy1.SendMessageNotification += OnSendMessageNotification;
// strategy2.SendMessageNotification += OnSendMessageNotification;

await strategy1.Init();
// await strategy2.Init();

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var bar = eventArgs.DeserializeToModel<Bar>();
    if (bar == null)
    {
        logger.Warning("Received bar was null after deserialization. body: {Body} | topic binding: {Binding}", eventArgs.BodyAsString(), eventArgs.RoutingKey);
        return;
    }
    
    strategy1.NewBar(bar);
    // strategy2.NewBar(bar);
};

await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;



void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}