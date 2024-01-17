using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;


[BackTestStrategyMetadata("Mama Fama", StartingBalance = 5000)]
public class MamaFama : BacktestRunner
{
    // public override DateTime FromDate { get; set; } = new DateTime(2019, 01, 01);
    // public override DateTime ToDate { get; set; } = new DateTime(2020, 01, 01);
    
    // public override DateTime FromDate { get; set; } = DateTime.Parse("2016-01-06 21:30");
    // public override DateTime ToDate { get; set; } = new DateTime(2017, 01, 01);
    
    private List<TickerData> _tickerData = new();

    private bool _tradeOpen;
    private bool _isLong = false;
    
    private decimal sl;
    private decimal tp;

    private double _slHit;
    private double _tpHit;
    
    public override Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        return Task.CompletedTask;
    }


    public override string ExtraDetails()
    {
        return $"""
                stop loss : {_slHit}
                take profit: {_tpHit}
                win % : {(_slHit > 0 && _tpHit > 0 ? _tpHit / (_slHit + _tpHit) * 100 : 0):F} %
                """;
    }

    private int tradeId;

    public override void OnFinish(BacktestState state)
    {
        if (_tradeOpen)
        {
            state.Trade.Margin.ClosePosition(tradeId);
        }
    }

    public override void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle().CandleToTicker();
        _tickerData.Add(candle);

        if (_tickerData.Count < 200)
        {
            return;
        }
        
        _tickerData.RemoveAt(0);


        if (_tradeOpen)
        {
            if(_isLong)
            {
                if ((decimal) candle.Close <= sl)
                {
                    state.Trade.Margin.ClosePosition(tradeId);
                    _tradeOpen = false;
                    _slHit++;
                    return;
                }
            } 
            else
            {
                if ((decimal) candle.Close >= sl)
                {
                    state.Trade.Margin.ClosePosition(tradeId);
                    _tradeOpen = false;
                    _slHit++;
                    return;
                }
            }
        }
        
        var stockData = new StockData(_tickerData);
    
        // var mmi = stockData.CalculateMarketMeannessIndex(MovingAvgType.HullMovingAverage);
        // var meanness = mmi.LatestValue("Mmi");
        // var isTrending = meanness < 75;
        var adx = stockData.CalculateAverageDirectionalIndex().LatestValue("Adx");
        var isTrending = adx > 25;
        
        stockData.Clear();
        
        var ehlersMotherAMA = stockData.CalculateEhlersMotherOfAdaptiveMovingAverages(
            fastAlpha: isTrending ? 0.5 : 0.25,
            slowAlpha: isTrending ? 0.05 : 0.04
        );
        var fama = ehlersMotherAMA.LatestValue("Fama");
        var mama = ehlersMotherAMA.LatestValue("Mama");
        
        var famaPrev = ehlersMotherAMA.OutputValues["Fama"][200 - 3];
        var mamaPrev = ehlersMotherAMA.OutputValues["Mama"][200 - 3];
        
        stockData.Clear();
        var atr = (decimal) stockData.CalculateAverageTrueRange().LatestValue("Atr");

        stockData.Clear();
        var rsi = stockData.CalculateEhlersAdaptiveRelativeStrengthIndexV1().LatestValue("Earsi");
            
        
        // borrow less in a ranging market
        var borrowAmount = isTrending ? 2.0m : 1.25m;

        var adaptiveTs = (decimal) stockData.CalculateAdaptiveTrailingStop().LatestValue("Ts");
        // mama has crossed from below
        if (mamaPrev < famaPrev && mama > fama)
        {
            if (_tradeOpen && !_isLong)
            {
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
            } 
            if (!_tradeOpen)
            {
                tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.BaseBalance * borrowAmount);
                _tradeOpen = true;
                
                sl = adaptiveTs;
            }
            
            
        }
        // mama has crossed under
        else if (mamaPrev > famaPrev && mama < fama)
        {
            if (_tradeOpen && _isLong)
            {
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
            } 
            if (!_tradeOpen)
            {
                tradeId = state.Trade.Margin.Short(AmountType.Absolute, state.BaseBalance * borrowAmount);
                _tradeOpen = true;
                _isLong = false;
                sl = adaptiveTs;
            }
            
        }
        else if (_tradeOpen)
        {
            // close short position and create new position
            if (!_isLong && rsi is < 20 and > 0)
            {
                state.AddLogEntry($"rsi short buy trigger : {rsi}");
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
                if (fama < mama)
                {
                    tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.BaseBalance * borrowAmount);
                    _tradeOpen = true;
                    sl = adaptiveTs;
                    _isLong = true;
                }
            } else if (_isLong && rsi > 90)
            {
                state.AddLogEntry($"rsi long sell trigger : {rsi}");
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
                
                if (fama > mama)
                {
                    tradeId = state.Trade.Margin.Short(AmountType.Absolute, state.BaseBalance * borrowAmount);
                    _tradeOpen = true;
                    _isLong = false;
                    sl = adaptiveTs;
                }
            }
        }
        
    }
}