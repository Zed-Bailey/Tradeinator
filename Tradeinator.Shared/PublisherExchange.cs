using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Tradeinator.Shared;

public class PublisherExchange: IDisposable
{
    private ConnectionFactory _factory;
    
    private readonly string _host;
    private readonly string _exchangeName;
    
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

    /// <summary>
    /// publish an object with topic to the exchange
    /// </summary>
    /// <param name="model">model to send. if type is string, will send the string else will json serialise the object</param>
    /// <param name="key">exchange topic</param>
    public void Publish(object model, string key)
    {
        string serialised;
        // if the model is already a string type, assume that it has already been serialised
        if (model is string s)
        {
            serialised = s;
        }
        else
        {
            serialised = JsonSerializer.Serialize(model);
        }
        
        // Console.WriteLine(serialised);
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