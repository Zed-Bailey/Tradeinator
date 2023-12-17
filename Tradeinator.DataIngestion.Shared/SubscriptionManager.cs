using Alpaca.Markets;
using Serilog.Core;
using Tradeinator.Shared;

namespace Tradeinator.DataIngestion.Shared;

public abstract class SubscriptionManager : IAsyncDisposable
{

    /// <summary>
    /// key is the streaming subscriptions symbol, e.g. AAPL
    /// </summary>
    private Dictionary<string, IAlpacaDataSubscription<IBar>> _subscriptions;
    
    private FileSystemWatcher _fileSystemWatcher;
    private Logger _logger;

    private PublisherExchange _exchange;

    /// <summary>
    /// path to the watched file
    /// </summary>
    public string SymbolsFile => Path.Combine(_fileSystemWatcher.Path, _fileSystemWatcher.Filter);
    
    public SubscriptionManager(Logger logger, PublisherExchange exchange, string directoryPath, string symbolsFileName)
    {
        _subscriptions = new();
        
        _logger = logger;
        _exchange = exchange;
        _exchange = exchange;
        
        
        _fileSystemWatcher = new FileSystemWatcher(directoryPath);
        _fileSystemWatcher.Filter = symbolsFileName;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.Changed += OnSymbolsFileChanged;
        
        // load file
        OnSymbolsFileChanged(new object(), new FileSystemEventArgs(WatcherChangeTypes.Changed, directoryPath, symbolsFileName));
    }
    
    public abstract ValueTask Subscribe(params IAlpacaDataSubscription<IBar>[] subscriptions);
    public abstract ValueTask UnSubscribe(params IAlpacaDataSubscription<IBar>[] subscriptions);
    public abstract IAlpacaDataSubscription<IBar> GetSubscription(string symbol);
    

    /// <summary>
    /// Un subscribe from all registered data streams
    /// </summary>
    /// <returns></returns>
    public Task UnsubscribeFromAll()
    {
        var subs = _subscriptions.Values;
        _logger.Information("Unsubscribing from all streams");
        return UnSubscribe(subs.ToArray()).AsTask();
    }

    
    
    private void OnSymbolsFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (eventArgs.ChangeType != WatcherChangeTypes.Changed) return;
        var text = File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "symbols.txt"));
    
        _logger.Information("Symbols file changed {Path} | {Event}", eventArgs.FullPath, eventArgs.ChangeType);
    
        // no symbols in file
        if (text.Length == 0)
        {
            // deleted all symbols in file, but still have subscriptions
            if (_subscriptions.Count > 0)
            {
                UnsubscribeFromAll().Wait();
            }

            return;
        }
    
        // loop through the subscripts and check the keys, if the key doesn't exist then the the 
        // streaming subscription is unsubscribed
        foreach (var key in _subscriptions.Keys)
        {
            if (text.Contains(key)) continue;
        
            
            UnSubscribe(_subscriptions[key]).AsTask().Wait();
            
            _logger.Information("Unsubscribed {Symbol} data subscription", key);

            _subscriptions.Remove(key);
        }

        // loop through all the lines, check if a subscription has been registered
        // if not, a new data subscription is created
        foreach (var line in text)
        {
            // trim whitespace and convert to uppercase
            var symbol = line.Trim().ToUpper();
            
            if (_subscriptions.ContainsKey(symbol)) continue;

            var subscription = BuildNewSubscription(symbol);
            
            Subscribe(subscription).AsTask().Wait();
            
            _logger.Information("Created a new subscription for {Symbol}", symbol);
            
            _subscriptions[symbol] = subscription;
        }
    }

    private IAlpacaDataSubscription<IBar> BuildNewSubscription(string symbol)
    {
        var newSubscription = GetSubscription(symbol);
        
        newSubscription.Received += bar =>
        {
            Console.WriteLine($"New {bar.Symbol} bar received");
            var msg = new
            {
                bar.Open,
                bar.High,
                bar.Low,
                bar.Close,
                bar.TimeUtc,
            };
                
            // publish bar to exchange
            _exchange.Publish(msg, $"bar.{bar.Symbol}");
        };

        newSubscription.OnSubscribedChanged += () =>  _logger.Information("Subscription changed for {Symbol}", symbol);
        
        return newSubscription;
    }

    public ValueTask DisposeAsync()
    {
        _fileSystemWatcher.Dispose();

        return UnSubscribe(_subscriptions.Values.ToArray());
    }
    
}