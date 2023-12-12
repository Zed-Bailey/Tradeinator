using Alpaca.Markets;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.Shared;

string host = "localhost";
string exchangeName = "test_exchange";

using var exchange = new PublisherExchange(host, exchangeName);

// initialise serilog logger, writing to console and file
using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("data_ingestion.log")
    .CreateLogger();

// load the dotenv file into the environment
DotEnv.LoadEnvFiles(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// load the config
var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true)
        .AddEnvironmentVariables()
        .Build();

//

var client = Environments.Paper
    .GetAlpacaTradingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));

var clock = await client.GetClockAsync();

Console.WriteLine(
    "Timestamp: {0}, NextOpen: {1}, NextClose: {2}",
    clock.TimestampUtc, clock.NextOpenUtc, clock.NextCloseUtc);


// var data = Environments.Paper.GetAlpacaDataStreamingClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));
// data.RegisterLoggers(logger);
//
//
// var applSubscription = data.GetMinuteBarSubscription("AAPL");
//
// var topic = "bar.aapl";
//
// applSubscription.Received += bar =>
// {
//     var msg = new
//     {
//         bar.Open,
//         bar.High,
//         bar.Low,
//         bar.Close
//     };
//
//     exchange.Publish(msg, topic);
// };
//
// await data.SubscribeAsync(applSubscription);
//
// Console.WriteLine(">> Press any key to exit");
// Console.ReadLine();
//
// await data.UnsubscribeAsync(applSubscription);

var data = Environments.Paper.GetAlpacaDataClient(new SecretKey(config["ALPACA_KEY"],config["ALPACA_SECRET"]));

var startDate = new DateTime(2021, 01, 01);
var endDate = new DateTime(2021, 2, 28);
var page = await data.ListHistoricalBarsAsync(
    new HistoricalBarsRequest("AAPL", startDate, endDate, BarTimeFrame.Day));


while (true)
{
    foreach (var bar in page.Items)
    {
        exchange.Publish(bar, "bar.aapl");
        Thread.Sleep(2000);
    }
    Console.Write("re run: [y]/[n]: ");
    if (Console.ReadLine() != "y") break;
}