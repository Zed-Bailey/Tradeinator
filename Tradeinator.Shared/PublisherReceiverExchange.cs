using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tradeinator.Shared;

/// <summary>
/// Combines the Publisher and Receiver Exchanges
/// </summary>
public class PublisherReceiverExchange: IDisposable
{
    private ConnectionFactory _factory;
    
    private readonly string _host;
    private readonly string _exchangeName;
    
    private IConnection _connection;
    private IModel _channel;
    
    private EventingBasicConsumer _consumer;
    public EventHandler<BasicDeliverEventArgs>? ConsumerOnReceive = null;
    
    private readonly string _queueName;
    private List<string> _bindingKeys;
    public string[] Bindings => _bindingKeys.ToArray();
    
    public PublisherReceiverExchange(string host, string exchangeName, params string[] bindingKeys)
    {
        if (bindingKeys.Length == 0)
        {
            throw new ArgumentException("1 or more topic binding keys are required");
        }
        
        _bindingKeys = new List<string>(bindingKeys);
        
        _host = host;
        _factory = new ConnectionFactory {HostName = _host};
        _exchangeName = exchangeName;

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(exchangeName, type: ExchangeType.Topic);

        _queueName = _channel.QueueDeclare().QueueName;
        
        // // bind the channel to all binding keys passed in
        // foreach (var key in bindingKeys)
        // {
        //     _channel.QueueBind(_queueName, _exchangeName, routingKey: key);
        // }

        _consumer = new EventingBasicConsumer(_channel);
    }

    /// <summary>
    /// Adds a new binding key, must be called before calling the Consume method
    /// </summary>
    /// <param name="key"></param>
    public void RegisterNewBindingKey(string key) => _bindingKeys.Add(key);

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
        
        // bind the channel to all binding keys passed in
        foreach (var key in _bindingKeys)
        {
            _channel.QueueBind(_queueName, _exchangeName, routingKey: key);
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