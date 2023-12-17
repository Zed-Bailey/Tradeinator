using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tradeinator.Shared;


public class ReceiverExchange: IDisposable
{
    private ConnectionFactory _factory;
    private IConnection _connection;
    private IModel _channel;

    private readonly string _queueName;
   
    private string[] _bindingKeys;
    public string[] Bindings => _bindingKeys;
    
    private string _host;
    private readonly string _exchangeName;
    
    private EventingBasicConsumer _consumer;
    public EventHandler<BasicDeliverEventArgs>? ConsumerOnReceive = null;

    

    /// <summary>
    /// Creates a new receiver exchange
    /// </summary>
    /// <param name="host">the exchange host</param>
    /// <param name="exchangeName">name of the exchange to connect to</param>
    /// <param name="bindingKeys">topic binding keys to listen for</param>
    /// <exception cref="ArgumentException">throws if no binding keys are passed in</exception>
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
        
        // bind the channel to all binding keys passed in
        foreach (var key in bindingKeys)
        {
            _channel.QueueBind(_queueName, _exchangeName, routingKey: key);
        }

        _consumer = new EventingBasicConsumer(_channel);
    }

    /// <summary>
    /// Registers the consumer callback to the channel and starts consuming
    /// returns a task that waits indefinitely or until the cancellation token is triggered
    /// </summary>
    /// <exception cref="ArgumentNullException">Throws if the ConsumerOnReceive callback hasn't been set</exception>
    public Task StartConsuming(CancellationToken token = default(CancellationToken))
    {
        if (ConsumerOnReceive == null)
        {
            throw new ArgumentNullException(nameof(ConsumerOnReceive));
        }
        
        _consumer.Received += ConsumerOnReceive;
        _channel.BasicConsume(_queueName, true, _consumer);

        return Task.Delay(-1, token);
    }
    


    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
    }
}