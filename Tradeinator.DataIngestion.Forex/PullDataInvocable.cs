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
    
    
    public async Task Invoke()
    {
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
