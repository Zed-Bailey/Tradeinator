using RabbitMQ.Client.Events;
using Tradeinator.Shared;

namespace Tradeinator.Dashboard.Data;


public class EventService
{
    public readonly string Host;
    public readonly string ExchangeName;
    private List<ExchangeEvent> _events = new();
    private ReceiverExchange _exchange;

    public List<ExchangeEvent> ExchangeEvents => _events;

    public event EventHandler NewDataReceived;

    public bool Connected = false;
    public string FailedConnectionMessage;
    
    public EventService(string host, string exchangeName)
    {
        Host = host;
        ExchangeName = exchangeName;

        try
        {
            _exchange = new ReceiverExchange(Host, ExchangeName, "#");
            _exchange.ConsumerOnReceive += ReceivedEvent;
            Connected = true;
        }
        catch (Exception e)
        {
            Connected = false;
            FailedConnectionMessage = e.Message;
        }
        
    }

    public async Task StartConsuming() {
        try
        {
            await _exchange.StartConsuming(returnIndefiniteTask: false);
            Connected = true;
        }
        catch (Exception e)
        {
            Connected = false;
            FailedConnectionMessage = e.Message;
        }
        
    }
    
    private void ReceivedEvent(object? sender, BasicDeliverEventArgs e)
    {
        var ee = new ExchangeEvent(e.RoutingKey, e.BodyAsString(), DateTime.Now, Guid.NewGuid().ToString());
        _events.Add(ee);
        // have max 100 previous events in the window
        if (_events.Count > 100)
        {
            _events.RemoveAt(0);
        }
        
        NewDataReceived?.Invoke(this, EventArgs.Empty);
    }
}