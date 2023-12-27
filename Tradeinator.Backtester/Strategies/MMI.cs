using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

public class MMI : IBacktestRunner
{
    private List<TickerData> _tickerData = new();
    
    public DateTime FromDate { get; set; } = new DateTime(2020, 01, 01);
    public DateTime ToDate { get; set; } = new DateTime(2023, 01, 01);
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
            return;
        }
        _tickerData.RemoveAt(0);
        
                    
        var stockData = new StockData(_tickerData);
        var mmi = stockData.CalculateMarketMeannessIndex(MovingAvgType.SimpleMovingAverage);
        foreach (var kvp in mmi.OutputValues)
        {
            state.AddLogEntry($"{kvp.Key} | {kvp.Value.Last()}");
        }
        stockData.Clear();
        
        var fallingRisingFilter = stockData.CalculateFallingRisingFilter();
        foreach (var kvp in fallingRisingFilter.OutputValues)
        {
            state.AddLogEntry($"{kvp.Key} | {kvp.Value.Last()}");
        }

    }
}
