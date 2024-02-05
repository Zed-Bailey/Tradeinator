// See https://aka.ms/new-console-template for more information

using System.Text;
using Tradeinator.Configuration;
using Tradeinator.Shared;


var config = new ConfigurationLoader();

var host = config["Rabbit:Host"];
var exchangeName = config["Rabbit:Exchange"];

// will listen to all events that contain topic 'bar.'
using var exchange = new ReceiverExchange(host, exchangeName, "bar.*");

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var m = Encoding.UTF8.GetString(body);
    Console.WriteLine($"{eventArgs.RoutingKey} | {m}");
};

Console.WriteLine("Exchange setup, ready to start consuming");
// will register the callback above to the channel and start consuming
await exchange.StartConsuming();