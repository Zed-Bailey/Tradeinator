using Serilog.Core;

namespace Tradeinator.DataIngestion.Shared;

/// <summary>
/// A simple subscription manager, simply watches a file and splits on the new line
/// </summary>
public class SimpleSubscriptionManager
{


    private List<string> _watchedSymbols = new();
    public List<string> Symbols => _watchedSymbols;
    
    private FileSystemWatcher _fileSystemWatcher;
    private Logger _logger;
    private string _directoryPath;
    public SimpleSubscriptionManager(Logger logger, string directoryPath, string symbolsFileName)
    {
        
        _logger = logger;
        _directoryPath = directoryPath;
        
        _fileSystemWatcher = new FileSystemWatcher(directoryPath);
        _fileSystemWatcher.Filter = symbolsFileName;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.Changed += OnSymbolsFileChanged;
        
        // load file
        OnSymbolsFileChanged(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, directoryPath, symbolsFileName));
    }
    
    
    private void OnSymbolsFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (eventArgs.ChangeType != WatcherChangeTypes.Changed) return;
        var text = File.ReadAllLines(Path.Combine(_directoryPath, "symbols.txt"));
    
        _logger.Information("Symbols file changed {Path} | {Event}", eventArgs.FullPath, eventArgs.ChangeType);
    
        // no symbols in file
        if (text.Length == 0)
        {
            // deleted all symbols in file, but still have subscriptions
            if (_watchedSymbols.Count > 0)
            {
                _watchedSymbols.Clear();
            }

            return;
        }
    
        // loop through the subscripts and check the keys, if the key doesn't exist then the the 
        // streaming subscription is unsubscribed
        foreach (var key in _watchedSymbols.ToArray())
        {
            if (text.Contains(key)) continue;
            
            _watchedSymbols.Remove(key);
            _logger.Information("Unsubscribed {Symbol} data subscription", key);
            
        }

        // loop through all the lines, check if a subscription has been registered
        // if not, a new data subscription is created
        foreach (var line in text)
        {
            // trim whitespace and convert to uppercase
            var symbol = line.Trim().ToUpper();
            
            if (_watchedSymbols.Contains(symbol)) continue;
            
            _logger.Information("Created a new subscription for {Symbol}", symbol);
            
            _watchedSymbols.Add(symbol);
        }
    }
}