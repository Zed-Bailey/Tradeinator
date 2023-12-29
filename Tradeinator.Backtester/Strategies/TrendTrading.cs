using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Trend Trading")]
public class TrendTrading : IBacktestRunner
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public BarTimeFrame TimeFrame { get; set; }

    private List<TickerData> _tickerData = new();

    bool _tradeOpen = false;
    
    public Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        return Task.CompletedTask;
    }

    public void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle();
        _tickerData.Add(candle.CandleToTicker());
        if (_tickerData.Count < 100)
        {
            return;
        }

        _tickerData.RemoveAt(0);

        var stockData = new StockData(_tickerData);
        var slowMa = stockData.CalculateExponentialMovingAverage(100).LatestValue("Ema");
        var fastMa = stockData.CalculateExponentialMovingAverage(24).LatestValue("Ema");
        stockData.Clear();
        
        var rsi = stockData.CalculateRelativeStrengthIndex().LatestValue("Rsi");
        
       

        // market not trending
        // if (mmi > 75) return;

        if (rsi < 30 && fastMa > slowMa)
        {
            if (!_tradeOpen)
            {
                state.Trade.Spot.Buy(AmountType.Percentage, 1);
                _tradeOpen = true;
                return;
            }
            
        }

        if (_tradeOpen)
        {
            if (rsi > 70 && fastMa < slowMa)
            {
                state.Trade.Spot.Sell();
                _tradeOpen = false;
            }
        }
        

    }
}