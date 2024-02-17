using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client.Events;
using Serilog.Core;
using Tradeinator.Configuration;
using Tradeinator.Database;
using Tradeinator.Database.Models;
using Tradeinator.Shared;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Shared.Models.Events;

namespace Tradeinator.Strategy.Shared;

public class StrategyBuilder<T>: IAsyncDisposable where T : StrategyBase, new() 
{
    private PublisherReceiverExchange _exchange;
    private int _maxStrategies;
    private string _slug;
    private EventHandler<SystemMessageEventArgs>? _messageNotificationCallback;
    private EventHandler? _barCallback;
    private string _connectionString;
    private string? _defaultSerialisedStrategy;
    private Logger _logger;
    public Dictionary<int, T> LoadedStrategies = new();

    private EventStateHandler _esh;
    private string[] _barBindings;
    
    public StrategyBuilder(string connectionString, Logger logger, params string[] barBindings)
    {
        _connectionString = connectionString;
        _logger = logger;
        _barBindings = barBindings;
    }

    /// <summary>
    /// add the exchange used. automatically registers a new binding for strategy updates
    /// </summary>
    /// <param name="exchange"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">throws if slug is not set</exception>
    public StrategyBuilder<T> WithExchange(PublisherReceiverExchange exchange)
    {
        _exchange = exchange;
        if (string.IsNullOrEmpty(_slug))
            throw new ArgumentException($"Slug must be set before calling {nameof(WithExchange)}");
        
        // register a listener for the update strategy event
        _exchange.RegisterNewBindingKey($"update.{_slug}");
        _logger.Information("Registered new binding for strategy updates: update.{Slug}", _slug);
        
        return this;
    }

    public StrategyBuilder<T> WithMax(int maxStrategies)
    {
        _maxStrategies = maxStrategies;
        return this;
    }

    /// <summary>
    /// register the strategy slug
    /// </summary>
    /// <param name="slug"></param>
    /// <returns></returns>
    public StrategyBuilder<T> WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }
    
    /// <summary>
    /// register the callback for notifications
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public StrategyBuilder<T> WithMessageNotificationCallback( EventHandler<SystemMessageEventArgs> callback)
    {
        _messageNotificationCallback = callback;
        return this;
    }
    
    
    /// <summary>
    /// default strategy used if none are found in the database
    /// </summary>
    /// <param name="defaultStrategy"></param>
    /// <returns></returns>
    public StrategyBuilder<T> WithDefaultStrategy(StrategyBase defaultStrategy)
    {
        var serialised = StrategyLoader.SerialiseStrategy(defaultStrategy);
        _defaultSerialisedStrategy = serialised;
        return this;
    }


    /// <summary>
    /// optional bar callback, invoked after calling the NewBar method on the strategies
    /// </summary>
    /// <param name="barCallback"></param>
    /// <returns></returns>
    public StrategyBuilder<T> WithBarCallback(EventHandler barCallback)
    {
        _barCallback = barCallback;
        return this;
    }
    
    /// <summary>
    /// Builds the strategies
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public StrategyBuilder<T> Build()
    {
        if (string.IsNullOrEmpty(_slug))
            throw new ArgumentException("slug must be defined. call the WithSlug method before calling build");
        
        using var context = DbContextCreator.CreateContext(_connectionString);

        var existingStrategies = context.SavedStrategies.Where(x => x.Slug == _slug).ToList();

        if (!existingStrategies.Any())
        {
            if (string.IsNullOrEmpty(_defaultSerialisedStrategy))
            {
                _logger.Warning("[WARN] No default strategy was specified, no strategies will be loaded");
            } 
            else
            {
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
            }
        }

      
        
        
        foreach (var serialisedConfig in existingStrategies)
        {
            var deserialised = StrategyLoader.LoadStrategy<T>(serialisedConfig.Config);
            deserialised.SendMessageNotification += _messageNotificationCallback;
            LoadedStrategies.Add(serialisedConfig.SavedStrategyId, deserialised);
            _logger.Information("Loaded strategy '{Name}', Last updated: {LastUpdated}", serialisedConfig.StrategyName, serialisedConfig.LastUpdated);
        }
        
        // register consumer
        _exchange.ConsumerOnReceive += NewEventReceived;
        _logger.Information("Registered exchange consumers");
        
        _esh = new EventStateHandler()
            .Default(eventData => _logger.Warning("Unknown event received: {Event}", eventData))
            .If<Bar>(_barBindings[0], o =>
            {
                try
                {
                    
                    var bar = (Bar?) o;
                    _logger.Information("Received new bar, {Bar}", bar);
                    if (bar == null)
                    {
                        _logger.Warning("Received bar was null after casting: {o}");
                    }

                    foreach (var strategy in LoadedStrategies.Values)
                    {
                        strategy.NewBar(bar);
                    }
                    
                    // _barCallback?.Invoke(this, bar);
                }
                catch (InvalidCastException e)
                {
                    _logger.Error(e, "Failed to cast the object to the Bar type");
                }
                catch (Exception e)
                {
                    _logger.Error(e, "something threw an exception");
                }
                
            })
            .If<UpdateStrategyEvent>($"update.{_slug}", o => Console.WriteLine("update event received"));
        
        
        return this;
    }

    private void NewEventReceived(object? sender, BasicDeliverEventArgs e)
    {
        _esh.Consume(e.RoutingKey, e.BodyAsString());
    }


    /// <summary>
    /// Calls the Init method of each strategy
    /// </summary>
    public async Task Init()
    {
        var config = new ConfigurationLoader();
        
        foreach (var strategy in LoadedStrategies.Values)
        {
            await strategy.Init(config);
        }
    }

    /// <summary>
    /// Will call the dispose method of each strategy
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var strategy in LoadedStrategies.Values)
        {
            await strategy.DisposeAsync();
        }
    }
}