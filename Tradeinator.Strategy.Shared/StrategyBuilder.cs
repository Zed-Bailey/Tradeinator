using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client.Events;
using Tradeinator.Database;
using Tradeinator.Database.Models;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Shared.Models.Events;

namespace Tradeinator.Strategy.Shared;

public class StrategyBuilder<T> where T : StrategyBase, new() 
{
    private PublisherReceiverExchange _exchange;
    private int _maxStrategies;
    private string _slug;
    private  EventHandler<SystemMessageEventArgs> _messageNotificationCallback;
    private EventHandler _barCallback;
    private string _connectionString;
    private string? _defaultSerialisedStrategy;
    
    public Dictionary<int, T> LoadedStrategies = new(); 
    
    
    public StrategyBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    public StrategyBuilder<T> WithExchange(PublisherReceiverExchange exchange)
    {
        _exchange = exchange;
        if (string.IsNullOrEmpty(_slug))
            throw new ArgumentException($"Slug must be set before calling {nameof(WithExchange)}");
        
        _exchange.RegisterNewBindingKey(_slug);
        
        return this;
    }

    public StrategyBuilder<T> WithMax(int maxStrategies)
    {
        _maxStrategies = maxStrategies;
        return this;
    }

    public StrategyBuilder<T> WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }
    
    public StrategyBuilder<T> WithMessageNotificationCallback( EventHandler<SystemMessageEventArgs> callback)
    {
        _messageNotificationCallback = callback;
        return this;
    }
    
    public StrategyBuilder<T> LoadFromDb()
    {
        return this;
    }
    
    public StrategyBuilder<T> ElseCreate(StrategyBase defaultStrategy)
    {
        var serialised = StrategyLoader.SerialiseStrategy(defaultStrategy);
        _defaultSerialisedStrategy = serialised;
        return this;
    }


    public StrategyBuilder<T> WithBarCallback(EventHandler barCallback)
    {
        _barCallback = barCallback;
        return this;
    }
    
    public StrategyBuilder<T> Build()
    {
        var builder = new DbContextOptionsBuilder<ApplicationContext>(); 
        builder.UseMySQL(_connectionString); 
        using var context = new ApplicationContext(builder.Options);

        var existingStrategies = context.SavedStrategies.Where(x => x.Slug == _slug).ToList();

        if (!existingStrategies.Any())
        {
            if (string.IsNullOrEmpty(_defaultSerialisedStrategy))
            {
                Console.WriteLine("[WARN] No default strategy was specified, no strategies will be loaded");
            }
            var savedDefaultStrat = new SavedStrategy
            {
                Slug = _slug,
                Config = _defaultSerialisedStrategy,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                StrategyName = "Default Strategy"
            };
            context.SavedStrategies.Add(savedDefaultStrat);
            context.SaveChanges();
            existingStrategies.Add(savedDefaultStrat);
            Console.WriteLine("This should not trigger");
        }

      
        
        
        foreach (var serialisedConfig in existingStrategies)
        {
            var deserialised = StrategyLoader.LoadStrategy<T>(serialisedConfig.Config);
            deserialised.SendMessageNotification += _messageNotificationCallback;
            LoadedStrategies.Add(serialisedConfig.SavedStrategyId, deserialised);
        }
        
        // register a listener for the update strategy event
        _exchange.RegisterNewBindingKey($"update.{_slug}");
        _exchange.ConsumerOnReceive += NewEventReceived;
        Console.WriteLine("Registered consumer");
        return this;
    }

    private void NewEventReceived(object? sender, BasicDeliverEventArgs e)
    {
        var bar = e.DeserializeToModel<Bar>();
        var update = e.DeserializeToModel<UpdateStrategyEvent>();
        
        if (bar != null)
        {
            Console.WriteLine($"received bar event, {bar} | {e.BodyAsString()}");
        }
        else if (update != null)
        {
            Console.WriteLine($"received update event {update}");
        }
        else
        {
            Console.WriteLine("Un recognised event received");
        }
        

    }


    public async Task Init()
    {
        foreach (var strategy in LoadedStrategies.Values)
        {
            await strategy.Init();
        }
    }
    
}