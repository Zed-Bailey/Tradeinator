using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tradeinator.Shared;


public class ReceiverExchange: IDisposable
{
    private ConnectionFactory _factory;
    private string _host;
    private string _exchangeName;
    
    private IConnection _connection;
    private IModel _channel;

    private string _queueName;
    private string[] _bindingKeys;

    private EventingBasicConsumer _consumer;

    public EventHandler<BasicDeliverEventArgs>? ConsumerOnReceive = null;

    


    public ReceiverExchange(string host, string exchangeName, params string[] bindingKeys)
    {
        if (bindingKeys.Length == 0)
        {
            throw new ArgumentException("1 or more topic binding keys are required");
        }
        
        _host = host;
        _exchangeName = exchangeName;
        _bindingKeys = bindingKeys;
        
        _factory = new ConnectionFactory { HostName = host };
        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(exchangeName, type: ExchangeType.Topic);

        _queueName = _channel.QueueDeclare().QueueName;
        foreach (var key in bindingKeys)
        {
            _channel.QueueBind(_queueName, _exchangeName, routingKey: key);
        }

        _consumer = new EventingBasicConsumer(_channel);
    }

    /// <summary>
    /// Registers the consumer to the channel, throws if consumer is null
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void StartConsuming()
    {
        if (ConsumerOnReceive == null)
        {
            throw new ArgumentNullException(nameof(ConsumerOnReceive));
        }
        
        _consumer.Received += ConsumerOnReceive;
        _channel.BasicConsume(_queueName, true, _consumer);
    }
    


    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
        
    }
}