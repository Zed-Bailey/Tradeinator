using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Stochastic scalping", StartingBalance = 500)]
public class StochasticScalping : BacktestRunner
{
    public override DateTime FromDate { get; set; } = new DateTime(2022, 06, 01);
    
    private List<TickerData> _tickerData = new();

    private bool _tradeOpen;

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
            if ((decimal)candle.Close < sl )
            {
                state.Trade.Spot.Sell();
                _tradeOpen = false;
                _slHit++;
                return;
            } 
            
            if ((decimal) candle.Close > tp)
            {
                state.Trade.Spot.Sell();
                _tradeOpen = false;
                _tpHit++;
                return;
            }
        }
        
        var stockData = new StockData(_tickerData);

        var ehlersMotherAMA = stockData.CalculateEhlersMotherOfAdaptiveMovingAverages(fastAlpha: 0.25);
        var fama = ehlersMotherAMA.LatestValue("Fama");
        var mama = ehlersMotherAMA.LatestValue("Mama");
        
        var emamaLongCondition = mama < fama;
        var emamaShortCondition = mama > fama;

        
        
        stockData.Clear();
        
        var stoch = stockData.CalculateStochasticOscillator(MovingAvgType.EhlersKaufmanAdaptiveMovingAverage);

        var k = stoch.OutputValues["FastK"].Last();
        var d = stoch.OutputValues["FastD"].Last();
        
        stockData.Clear();
        var atr = (decimal) stockData.CalculateAverageTrueRange().LatestValue("Atr");
        
        var stochLongCondition = (k < 20 && d < 20 && k > d) && emamaLongCondition;
        var stochShortCondition = (k > 80 && d > 80) || k < d;

        var longCondition = stochLongCondition;
        var shortCondition = stochShortCondition; 
        if(longCondition && !_tradeOpen)
        {
            // only risk 2% of equity per trade
            state.Trade.Spot.Buy(AmountType.Percentage, 2);
            _tradeOpen = true;
            var last = state.GetLastSpotTrade();
            tp = last.QuotePrice + atr;
            sl = last.QuotePrice - atr;
        }
        else if (_tradeOpen && shortCondition)
        {
            state.Trade.Spot.Sell();
            _tradeOpen = false;
             
        }
    }
}