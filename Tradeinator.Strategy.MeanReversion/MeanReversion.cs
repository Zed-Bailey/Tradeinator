using Alpaca.Markets;
using Serilog.Core;
using Tradeinator.Shared;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.MeanReversion;

public class MeanReversion : StrategyBase
{
    private List<decimal> _closingPrices = new();
    
    private IAlpacaCryptoDataClient _alpacaCryptoDataClient;

    private ITradeManager _tradeManager;
    
    public MeanReversion(ITradeManager manager, string key, string secret, params string[] symbols)
    {
        _tradeManager = manager;
        _alpacaCryptoDataClient = Environments.Paper.GetAlpacaCryptoDataClient(new SecretKey(key, secret));
    }
    


    public override async Task Init()
    {
        // var client = (IAlpacaTradingClient) _tradeManager.GetUnderlyingClient();
        // var s = await _alpacaTradingClient.ListIntervalCalendarAsync(
        //     new CalendarRequest().WithInterval(DateTime.Today.GetIntervalFromThat()));
        // foreach (var c in s)
        // {
        //     Console.WriteLine("{0} | {1}", c.GetTradingDate(), c.GetTradingCloseTimeUtc().ToLocalTime());
        //     
        // }
        
        Log("Initialised strategy");
    }

    public override void NewBar(Bar bar)
    {
        _closingPrices.Add(bar.Close);
        if (_closingPrices.Count < 20)
        {
            Log("waiting for more information");
            return;
        }
        
        _closingPrices.RemoveAt(0);
        var avg = _closingPrices.Average();
        var diff = avg - bar.Close;
        
    }

    public override void Dispose()
    {
        
    }
}