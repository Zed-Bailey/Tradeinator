using Coravel.Invocable;
using Serilog.Core;
using Tradeinator.DataIngestion.Shared;
using Tradeinator.Shared;

namespace Tradeinator.DataIngestion.Commodity;

public class PullDataInvocable : IInvocable
{
    private SimpleSubscriptionManager _subscriptionManager;
    private PublisherExchange _exchange;
    private Logger _logger;
    private OandaConnection _oandaConnection;
    
    public PullDataInvocable(SimpleSubscriptionManager subscriptionManager, PublisherExchange exchange, Logger logger, OandaConnection oandaConnection)
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
        var time = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/New_York");
        
        // closes friday at 16:59
        if (time.DayOfWeek is DayOfWeek.Friday && time.Hour > 17) return false;
        
        // market is not open on saturday
        if (time.DayOfWeek is DayOfWeek.Saturday) return false;

        // opens sunday at 18:05
        if (time.DayOfWeek is DayOfWeek.Sunday && time.Hour >= 18 && time.Minute > 5) return true;
        
        
        
        // otherwise open 24 hours a day
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