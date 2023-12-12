using Alpaca.Markets;
using Serilog.Core;
using Tradeinator.Shared;

namespace DataIngestion;

public class SubscriptionManager : IAsyncDisposable
{

    /// <summary>
    /// key is the streaming subscriptions symbol, e.g. AAPL
    /// </summary>
    private Dictionary<string, IAlpacaDataSubscription<IBar>> _subscriptions;
    
    private FileSystemWatcher _fileSystemWatcher;
    private Logger _logger;

    private PublisherExchange _exchange;
    private IAlpacaDataStreamingClient _client;

    /// <summary>
    /// path to the watched file
    /// </summary>
    public string SymbolsFile => Path.Combine(_fileSystemWatcher.Path, _fileSystemWatcher.Filter);
    
    public SubscriptionManager(Logger logger, PublisherExchange exchange, IAlpacaDataStreamingClient client, string directoryPath, string symbolsFileName)
    {
        _subscriptions = new();
        
        _logger = logger;
        _exchange = exchange;
        _exchange = exchange;
        _client = client;
        
        _fileSystemWatcher = new FileSystemWatcher(directoryPath);
        _fileSystemWatcher.Filter = symbolsFileName;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.Changed += OnSymbolsFileChanged;
        
        // todo load file and populate
    }
    
    
    private async void OnSymbolsFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (eventArgs.ChangeType != WatcherChangeTypes.Changed) return;
        var text = await File.ReadAllLinesAsync(Path.Combine(Directory.GetCurrentDirectory(), "symbols.txt"));
    
        _logger.Information("Symbols file changed {Path} | {Event}", eventArgs.FullPath, eventArgs.ChangeType);
    
        // no symbols in file
        if (text.Length == 0) return;
    
        // loop through the subscripts and check the keys, if the key doesn't exist then the the 
        // streaming subscription is unsubscribed
        foreach (var key in _subscriptions.Keys)
        {
            if (text.Contains(key)) continue;
        
            await _client.UnsubscribeAsync(_subscriptions[key]);
            _subscriptions.Remove(key);
        }

        // loop through all the lines, check if a subscription has been registered
        // if not, a new data subscription is created
        foreach (var line in text)
        {
            if (_subscriptions.ContainsKey(line)) continue;
        
            var newSubscription = _client.GetMinuteBarSubscription(line);
            
            newSubscription.Received += bar =>
            {
                var msg = new
                {
                    bar.Open,
                    bar.High,
                    bar.Low,
                    bar.Close
                };
                
                // publish bar to exchange
                _exchange.Publish(msg, $"bar.{bar.Symbol}");
            };

            await _client.SubscribeAsync(newSubscription);
            _subscriptions[line] = newSubscription;
        }
    }

    public ValueTask DisposeAsync()
    {
        _fileSystemWatcher.Dispose();

        return _client.UnsubscribeAsync(_subscriptions.Values);
    }
    
}