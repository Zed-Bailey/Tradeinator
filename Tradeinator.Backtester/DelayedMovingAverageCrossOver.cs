using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;

namespace Tradeinator.Backtester;

public class DelayedMovingAverageCrossOver : IBacktestRunner
{
    private DateTime? _nextTradeTime;
    private List<TickerData> _tickerData = new();
    
    private int? _positionId;

    public async Task InitStrategy(string symbol, DateTime startDate, IAlpacaCryptoDataClient dataClient)
    {
        var from = startDate.AddHours(-100);

        var data = await dataClient.ListHistoricalBarsAsync(new HistoricalCryptoBarsRequest(symbol, from, startDate, BarTimeFrame.Hour));
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
        // _data = new StockData(bars.Select(x => (double) x.Open), bars.Select(x => (double)x.High), bars.Select(x => (double)x.Low), bars.Select(x => (double)x.Close), bars.Select(x => (double)x.Volume), bars.Select(x => x.TimeUtc));
        // Console.WriteLine(_data.Count);
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