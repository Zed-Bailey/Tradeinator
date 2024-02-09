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
    
    
    public StrategyBuilder(string connectionString, Logger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
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
        return this;
    }

    private void NewEventReceived(object? sender, BasicDeliverEventArgs e)
    {
        var bar = e.DeserializeToModel<Bar>();
        var update = e.DeserializeToModel<UpdateStrategyEvent>();
        
        // todo: update to switch case with cast?
        
        if (update != null)
        {
            _logger.Information("Received new strategy update event, {Event}", update);
            var strategyToUpdate = LoadedStrategies[update.Id];
            using var context = DbContextCreator.CreateContext(_connectionString);
            var latest = context.SavedStrategies.Find(update.Id);
            if (latest == null)
            {
                _logger.Error("tried to load latest strategy changes for strategy with id {Id} but the model was null", update.Id);
                return;
            }
            
            StrategyLoader.UpdateStrategyProperties(strategyToUpdate, latest.Config);
            _logger.Information("Updated strategy with id {Id}", update.Id);
        }
        else if (bar != null)
        {
            _logger.Information("Received new bar, {Bar}", bar);
            foreach (var value in LoadedStrategies.Values)
            {
                value.NewBar(bar);
            }
            _barCallback?.Invoke(this, e);
        }
        else
        {
            _logger.Warning("Received an unrecognised event, {Event}", e.BodyAsString());
        }
        

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