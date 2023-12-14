using Serilog;
using Tradeinator.Shared;
using Tradeinator.Shared.Models;

// implementation of the mean reversion strategy from
// https://github.com/alpacahq/alpaca-trade-api-csharp/blob/develop/UsageExamples/MeanReversionPaperOnly.cs

const string host = "localhost";
const string exchangeName = "test_exchange";

using var exchange = new ReceiverExchange(host, exchangeName, "bar.ETH/USD");
var tokenSource = new CancellationTokenSource();


 // initialise serilog logger, writing to console and file
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("mean_reversion.log")
    .CreateLogger();


// handle ctrl-c
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancellation requested");
    tokenSource.Cancel();
};

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var bar = eventArgs.SerializeToModel<Bar>();
    Console.WriteLine(eventArgs.BodyAsString());
};


await exchange.StartConsuming();