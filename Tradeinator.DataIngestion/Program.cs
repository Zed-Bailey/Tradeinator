// See https://aka.ms/new-console-template for more information


using Tradeinator.Shared;

string host = "localhost";
string exchangeName = "test_exchange";

using var exchange = new PublisherExchange(host, exchangeName);

Console.WriteLine("send message in form: msg | topic\nenter 'quit' to exit");
Console.Write("> ");

string? input;
while ((input = Console.ReadLine()) != "quit")
{
    var split = input.Split("|");
    if(split.Length != 2) continue;

    var msg = split[0].Trim();
    var topic = split[1].Trim();
    Console.WriteLine($"Sending message '{msg}' to exchanges listening to topic: '{topic}'");
    
    exchange.Publish(msg, topic);
    
    Console.Write("> ");
}