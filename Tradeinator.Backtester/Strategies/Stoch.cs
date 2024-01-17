using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;
using Tradeinator.Shared.Extensions;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Stoch v2")]
public class Stoch : BacktestRunner
{
    public override DateTime FromDate { get; set; } = DateTime.Parse("2016-01-06 21:30");

    public override Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient)
    {
        return Task.CompletedTask;
    }

    private int? tradeId;
    private decimal atr;
    private decimal openPrice;
    
    private List<TickerData> _data = new();
    public override void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle().CandleToTicker();
        _data.Add(candle);

        if(_data.Count < 200) return;
        _data.RemoveAt(0);


        if (tradeId != null)
        {
            var top = openPrice + (this.atr * 2.5m);
            var btm = openPrice - (this.atr * 2.5m);

            if (candle.Close >= (double) top)
            {
                state.Trade.Margin.ClosePosition(tradeId.Value);
                state.AddLogEntry($"T/P triggered for {tradeId}");
                tradeId = null;
            }
            
            if (candle.Close <= (double) btm)
            {
                state.Trade.Margin.ClosePosition(tradeId.Value);
                state.AddLogEntry($"S/L triggered for {tradeId}");
                tradeId = null;
                
            }
        }
        
        
        var stockData = new StockData(_data);
        var candleHeight = (candle.Close - candle.Low) / (candle.High - candle.Low) * 100;

        var macd = stockData.CalculateMovingAverageConvergenceDivergence();
        var macdTrigger = macd.LatestValue("Macd") < macd.LatestValue("Histogram");
        macd.OutputValues["Histogram"].Reverse();
        var hasBottomed =
            macd.OutputValues["Histogram"][0] < 0 && macd.OutputValues["Histogram"][2] < macd.OutputValues["Histogram"][1]
                                                  && macd.OutputValues["Histogram"][0] < macd.OutputValues["Histogram"][1];
        stockData.Clear();
        
        var stochRsi =
            stockData.CalculateStochasticOscillator(MovingAvgType.WildersSmoothingMethod, 14, 1, 3);
        var stochTrigger = stochRsi.LatestValue("FastD") > 20 && stochRsi.LatestValue("FastD") < 50;

        stockData.Clear();
        
        var newAtr = stockData.CalculateAverageTrueRange(length: 7);
        
        if (candleHeight > 10 && macdTrigger && stochTrigger && hasBottomed)
        {
            if (tradeId == null)
            {
                tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.QuoteBalance * 0.5m);
                atr = (decimal) newAtr.LatestValue("Atr");
                openPrice = state.GetAllMarginTrades()[tradeId.Value].OpenPrice;
            }
            
        }
        


    }
}