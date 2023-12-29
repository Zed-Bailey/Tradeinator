using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Delayed Moving average crossover")]
public class DelayedMovingAverageCrossOver : IBacktestRunner
{
    private DateTime? _nextTradeTime;
    private List<TickerData> _tickerData = new();
    
    private int? _positionId;

    private bool _positionOpen = false;
    public DateTime FromDate { get; set; } = new DateTime(2020, 01, 01);
    public DateTime ToDate { get; set; } = new DateTime(2023, 01, 01);

    private decimal qty = 0;
    private decimal scale = 200;
    
    public BarTimeFrame TimeFrame { get; set; } = BarTimeFrame.Hour;

    public async Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        var from = FromDate.AddMonths(-1);

        var data = await dataClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest(symbol, from, FromDate, BarTimeFrame.Hour));
        var bars = data.Items;
        foreach (var b in bars)
        {
            _tickerData.Add(new TickerData
            {
                Open = (double) b.Open,
                High = (double) b.High,
                Low = (double) b.Low,
                Close = (double) b.Close,
                Volume = (double) b.Volume,
                Date = b.TimeUtc
            });
        }
    }

    public void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle();
        _tickerData.Add(candle.CandleToTicker());
        if (_tickerData.Count < 100)
        {
            state.AddLogEntry($"Not enough data: {candle.Time}");
            return;
        }
        
        if (_tickerData.Count > 100)
        {
            _tickerData.RemoveAt(0);    
        }


        if (_nextTradeTime != null && candle.Time >= _nextTradeTime)
        {
            var currentPrice = candle.Close;
            var positionValue = 0m;
            if (qty > 0)
            {
                positionValue = qty * currentPrice;
            }
            
            var diff = (decimal)_tickerData.Average(c => c.Close) - currentPrice;
            var pShare = diff / currentPrice * scale;
            var targetPositionValue = state.QuoteBalance * pShare;
            var positionSize = targetPositionValue - positionValue;

            // if (positionSize > 0)
            // {
                // var s = state.Trade.Spot.Buy(AmountType.Absolute, positionSize);
                var s = state.Trade.Spot.Buy();
                if (s)
                {
                    
                    // qty = positionSize;
                    _nextTradeTime = null;
                    // state.AddLogEntry($"Successfully bought {qty}");
                    _positionOpen = true;
                }
            // }
            // else
            // {
            //     state.AddLogEntry($"did not buy, position size was: {positionSize}");
            // }
           
            
            return;
        }
        
        var stockData = new StockData(_tickerData);
        
        var shortMA = stockData.CalculateSimpleMovingAverage(50).LatestValue("Sma");
        stockData.Clear();
        var longMA = stockData.CalculateSimpleMovingAverage(100).LatestValue("Sma");
        
        
        
        if (_nextTradeTime == null && shortMA > longMA && !_positionOpen)
        {
            _nextTradeTime = candle.Time.AddHours(5);
            state.AddLogEntry($"short cross long, setting next trade time to: {_nextTradeTime} from: {candle.Time}");
            return;
        }

        if (longMA > shortMA)
        {
            
            if (_positionOpen)
            {
                state.AddLogEntry("long cross short");
                // if(qty <= 0) return;
                
                // var s = state.Trade.Spot.Sell(AmountType.Absolute, qty);
                var s = state.Trade.Spot.Sell();
                if (s)
                {
                    state.AddLogEntry($"Closed position size of {qty} @ ${candle.Close}");
                    qty = 0;
                    _positionOpen = false;
                }
                else
                {
                    state.AddLogEntry($"Failed to close position of size: {qty}");
                }
                
            }
            
        }
        
        
    }
}


/*
if (_nextTradeTime != null && candle.Time >= _nextTradeTime)
{
            
    _positionId = state.Trade.Margin.Long();
    state.AddLogEntry($"Opening long position : id={_positionId}");
    _nextTradeTime = null;
            
    return;
}
        
var stockData = new StockData(_tickerData);
        
var shortMA = stockData.CalculateSimpleMovingAverage(50).LatestValue("Sma");
stockData.Clear();
var longMA = stockData.CalculateSimpleMovingAverage(100).LatestValue("Sma");
        
        
        
if (_nextTradeTime == null && shortMA > longMA && _positionId == null)
{
            
    _nextTradeTime = candle.Time.AddHours(5);
    state.AddLogEntry($"short cross long, setting next trade time to: {_nextTradeTime} from: {candle.Time}");
    return;
}

if (longMA > shortMA)
{
    state.AddLogEntry("long cross short");
    if (_positionId != null)
    {
        state.AddLogEntry($"closing position {_positionId}");
        state.Trade.Margin.ClosePosition(_positionId.Value);
        _positionId = null;
    }
            
}
*/