using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Double Z Score")]
public class DoubleZScore: BacktestRunner
{
    private List<TickerData> _data = new();
    // public override DateTime FromDate { get; set; } = new DateTime(2022, 06, 01);
    private bool _open = false;
    
    public override Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        return Task.CompletedTask;
    }


    
    public override void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle().CandleToTicker();
        _data.Add(candle);
        if (_data.Count < 200)
        {
            return;
        }
        
        _data.RemoveAt(0);
        
        var stockData = new StockData(_data);
        
        var longZ = stockData.CalculateFastZScore(length: 72).LatestValue("Fzs") * 10;
        var shortZ = stockData.CalculateFastZScore(length: 24).LatestValue("Fzs") * 10;
        
        if (shortZ < longZ && shortZ < -1 && longZ < -1)
        {
            if(!_open)
            {
                state.Trade.Spot.Buy(AmountType.Percentage, 5);
                _open = true;
            }
        }

        if (shortZ > longZ && shortZ > 2 && longZ > 2)
        {
            if(_open)
            {
                state.Trade.Spot.Sell();
                _open = false;
            }
        }
            
        
    }
}