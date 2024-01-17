using System.Runtime.InteropServices;
using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Shared.Extensions;

namespace Tradeinator.Backtester.Strategies;


[BackTestStrategyMetadata("Mama Fama", StartingBalance = 5000)]
public class MamaFama : BacktestRunner
{
    public override DateTime FromDate { get; set; } = new DateTime(2019, 06, 01);
    public override DateTime ToDate { get; set; } = new DateTime(2020, 01, 01);
    
    // public override DateTime FromDate { get; set; } = DateTime.Parse("2016-01-06 21:30");
    // public override DateTime ToDate { get; set; } = new DateTime(2017, 01, 01);
    
    private List<TickerData> _tickerData = new();

    private bool _tradeOpen;
    private bool _isLong = false;
    private int tradeId;
    
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
        
        var adx = stockData.CalculateAverageDirectionalIndex().LatestValue("Adx");
        var isTrending = adx > 25;
        
        stockData.Clear();
        
        var ehlersMotherAMA = stockData.CalculateEhlersMotherOfAdaptiveMovingAverages(
            fastAlpha: isTrending ? 0.5 : 0.25,
            slowAlpha: isTrending ? 0.05 : 0.04
        );

        var famaMamaCrossOver = ehlersMotherAMA.CrossOver("Mama", "Fama");
        var famaMamaCrossUnder = ehlersMotherAMA.CrossUnder("Mama", "Fama");

        stockData.Clear();
        
        // borrow less in a ranging market
        var borrowAmount = isTrending ? 2.0m : 1.25m;

        var adaptiveTs = (decimal) stockData.CalculateAdaptiveTrailingStop().LatestValue("Ts");
        // mama has crossed from below
        if (famaMamaCrossOver)
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
                _isLong = true;
                sl = adaptiveTs;
            }

            return;
        }
        // mama has crossed under
        if (famaMamaCrossUnder)
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

            return;

        }
        
        
        SecondaryTrigger(state, stockData, borrowAmount, adaptiveTs);
        
    }

    private void SecondaryTrigger(BacktestState state, StockData stockData, decimal borrowAmount, decimal stopLossPrice)
    {
        stockData.Clear();
        var rrsi = stockData.CalculateReverseEngineeringRelativeStrengthIndex(rsiLevel: 50);
        
        
        var latest = rrsi.LatestValue("Rersi");
        // close crosses over
        if (stockData.ClosePrices[^2] < latest && stockData.ClosePrices[^1] > latest)
        {
            if (!_tradeOpen)
            {
                state.AddLogEntry("close crossover");
                _isLong = true;
                _tradeOpen = true;
                tradeId = state.Trade.Margin.Long(AmountType.Absolute, borrowAmount);
                sl = stopLossPrice;
                return;
            }
        }

      
        // close crosses under 
        if (stockData.ClosePrices[^2] > latest && stockData.ClosePrices[^1] < latest)
        {
            if (_tradeOpen && _isLong)
            {
                   
                state.AddLogEntry("close crossunder");
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
            }
        }

    }
}