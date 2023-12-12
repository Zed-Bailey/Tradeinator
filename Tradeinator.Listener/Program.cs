// See https://aka.ms/new-console-template for more information

using System.Text;
using Tradeinator.Shared;

string host = "localhost";
string exchangeName = "test_exchange";

using var exchange = new ReceiverExchange(host, exchangeName, "bar.aapl");
exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var body = eventArgs.Body.ToArray();
    var m = Encoding.UTF8.GetString(body);
    Console.WriteLine($"{m}");
};

exchange.StartConsuming();

Console.WriteLine("Press any key to quit.");
Console.ReadLine();