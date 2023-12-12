using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Tradeinator.Shared;

public class PublisherExchange: IDisposable
{
    private ConnectionFactory _factory;
    private string _host;
    private string _exchangeName;
    
    private IConnection _connection;
    private IModel _channel;
    
    
    public PublisherExchange(string host, string exchangeName)
    {
        _host = host;
        _factory = new ConnectionFactory {HostName = _host};
        _exchangeName = exchangeName;

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(exchangeName, type: ExchangeType.Topic);

    }

    public void Publish(object model, string key)
    {
        var serialised = JsonSerializer.Serialize(model);
        Console.WriteLine(serialised);
        var body = Encoding.UTF8.GetBytes(serialised);
        
        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: key,
            basicProperties: null,
            body: body
        );
    }

    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
    }
}