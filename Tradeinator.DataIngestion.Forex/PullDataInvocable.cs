using Coravel.Invocable;
using Serilog.Core;
using Tradeinator.DataIngestion.Shared;
using Tradeinator.Shared;

namespace Tradeinator.DataIngestion.Forex;

public class PullDataInvocable : IInvocable
{
    private ForexSubscriptionManager _subscriptionManager;
    private PublisherExchange _exchange;
    private Logger _logger;
    private OandaConnection _oandaConnection;
    
    public PullDataInvocable(ForexSubscriptionManager subscriptionManager, PublisherExchange exchange, Logger logger, OandaConnection oandaConnection)
    {
        _subscriptionManager = subscriptionManager;
        _exchange = exchange;
        _logger = logger;
        _oandaConnection = oandaConnection;
    }

    /// <summary>
    /// Checks whether the market is open or not
    /// </summary>
    /// <returns>true when market is open, false otherwise</returns>
    private bool CheckTime()
    {
        var time = DateTime.UtcNow;
        // market is not open on saturday
        if (time.DayOfWeek is DayOfWeek.Saturday) return false;
            
        // market closes friday at 5pm ET or 10pm UTC
        if (time.DayOfWeek is DayOfWeek.Friday && time.Hour >= 22) return false;
            
        // market opens on sunday at 5pm ET or 10pm UTC
        if(time.DayOfWeek is DayOfWeek.Sunday && time.Hour >= 22) return true;
            
            
        return true;
    }
    
    
    public async Task Invoke()
    {
        if (!CheckTime())
            return;
        
        foreach (var symbol in _subscriptionManager.Symbols)
        { 
            var bar = await _oandaConnection.GetLatestData(symbol);
            if (bar is null)
            {
                _logger.Warning("Received bar for {Symbol} was null", symbol);
                continue;
            }
        
            // post bar to exchange
            _exchange.Publish(bar, $"bar.{symbol}");
            _logger.Information("Received bar for {Symbol} | {Date} | {O} {H} {L} {C}", 
                symbol, bar.TimeUtc, bar.Open, bar.High, bar.Low, bar.Close
            );
        
        }
    }
    
}
