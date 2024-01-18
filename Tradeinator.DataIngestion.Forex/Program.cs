using Coravel;
using Serilog;
using Tradeinator.DataIngestion.Forex;
using Tradeinator.DataIngestion.Shared;
using Tradeinator.Shared;

var config = new ConfigurationLoader(AppContext.BaseDirectory);
config.LoadConfiguration();

var exchangeHost = config.Get("Rabbit:Host");
var exchangeName = config.Get("Rabbit:Exchange");

if (exchangeHost is null || exchangeName is null)
{
    throw new ArgumentNullException(exchangeHost ?? exchangeName);
}

using var exchange = new PublisherExchange(exchangeHost, exchangeName);

// initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("data_ingestion.log")
    .CreateLogger();



//
var subscriptionManager = new ForexSubscriptionManager(logger, Directory.GetCurrentDirectory(), "symbols.txt");

var apiToken = config.Get("OANDA_API_TOKEN") ?? throw new ArgumentException("Oanda api token was null or empty");
var oandaConnection = new OandaConnection(apiToken);

var builder = Host.CreateApplicationBuilder(args);

// add services for worker
builder.Services.AddSingleton(subscriptionManager);
builder.Services.AddSingleton(exchange);
builder.Services.AddSingleton(logger);
builder.Services.AddSingleton(oandaConnection);

builder.Services.AddTransient<PullDataInvocable>();

builder.Services.AddScheduler();

var host = builder.Build();

host.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<PullDataInvocable>()
        .EveryThirtyMinutes();
});

host.Run();

logger.Information("Shutting down");

