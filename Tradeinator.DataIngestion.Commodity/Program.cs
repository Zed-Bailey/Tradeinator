using Coravel;
using Tradeinator.Configuration;
using Tradeinator.DataIngestion.Commodity;
using Tradeinator.DataIngestion.Shared;
using Serilog;
using Tradeinator.Shared;

var config = new ConfigurationLoader();

var exchangeHost = config.Get("Rabbit:Host");
var exchangeName = config.Get("Rabbit:Exchange");

if (exchangeHost is null || exchangeName is null)
{
    throw new ArgumentNullException(exchangeHost ?? exchangeName);
}

// using var exchange = new PublisherExchange(exchangeHost, exchangeName);

// initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("data_ingestion.log")
    .CreateLogger();



//
// var subscriptionManager = new SimpleSubscriptionManager(logger, Directory.GetCurrentDirectory(), "symbols.txt");

var apiToken = config.Get("OANDA_API_TOKEN") ?? throw new ArgumentException("Oanda api token was null or empty");
var oandaConnection = new OandaConnection(apiToken);

var bar = await oandaConnection.GetLatestData("XAU_AUD");
logger.Information("Received bar for {Symbol} | {Date} | {O} {H} {L} {C}", 
    "Gold/AUD", bar.TimeUtc, bar.Open, bar.High, bar.Low, bar.Close
);

//
// var builder = Host.CreateApplicationBuilder(args);
//
// // add services for worker
// builder.Services.AddSingleton(subscriptionManager);
// builder.Services.AddSingleton(exchange);
// builder.Services.AddSingleton(logger);
// builder.Services.AddSingleton(oandaConnection);
//
// builder.Services.AddTransient<PullDataInvocable>();
//
// builder.Services.AddScheduler();
//
// var host = builder.Build();
//
// host.Services.UseScheduler(scheduler =>
// {
//     scheduler
//         .Schedule<PullDataInvocable>()
//         .EveryThirtyMinutes();
//
// });
//
// host.Run();
//
// logger.Information("Shutting down");

