using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

public class DelayedMovingAverageCrossOver : IBacktestRunner
{
    private DateTime? _nextTradeTime;
    private List<TickerData> _tickerData = new();
    
    private int? _positionId;

    public DateTime FromDate { get; set; } = DateTime.Today.AddHours(-10);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public async Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        var from = FromDate.AddHours(-100);

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
        
        if (_tickerData.Count > 100)
        {
            _tickerData.RemoveAt(0);    
        }


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
            // _nextTradeTime = candle.Time.AddDays(2);
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
        
        
    }
}