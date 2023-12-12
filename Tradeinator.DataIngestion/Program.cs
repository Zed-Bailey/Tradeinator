// See https://aka.ms/new-console-template for more information


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


Console.WriteLine("send message in form: msg | topic\nenter 'quit' to exit");
Console.Write("> ");

string? input;
while ((input = Console.ReadLine()) != "quit")
{
    var split = input.Split("|");
    if(split.Length != 2) continue;

    var msg = split[0].Trim();
    var topic = split[1].Trim();
    logger.Information("Sending message '{@Msg}' to exchanges listening to topic: '{@Topic}'", msg, topic);
    
    exchange.Publish(msg, topic);
    
    Console.Write("> ");
}