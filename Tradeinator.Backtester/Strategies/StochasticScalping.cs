using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Shared.Extensions;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Stochastic scalping", StartingBalance = 5000)]
public class StochasticScalping : BacktestRunner
{
    // public override DateTime FromDate { get; set; } = new DateTime(2022, 06, 01);
    public override DateTime FromDate { get; set; } = DateTime.Parse("2016-01-06 21:30");

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

    private int tradeId;
    
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
            if ((decimal)candle.Close <= sl )
            {
                // state.Trade.Spot.Sell();
                state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
                _slHit++;
                return;
            } 
            
            if ((decimal) candle.Close >= tp)
            {
                state.Trade.Margin.ClosePosition(tradeId);
                // state.Trade.Spot.Sell();
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
            // state.Trade.Spot.Buy(AmountType.Percentage, 2);
            tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.QuoteBalance * 0.25m);
            _tradeOpen = true;
            // var last = state.GetLastSpotTrade();
            var last = state.GetAllMarginTrades()[tradeId].OpenPrice;
            tp = last + atr*3.5m;
            sl = last - atr*3.5m;
        }
        // else if (_tradeOpen && shortCondition)
        // {
        //     // state.Trade.Spot.Sell();
        //     state.Trade.Margin.ClosePosition(tradeId);
        //     _tradeOpen = false;
        //      
        // }
    }
}