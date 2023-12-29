using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Hull Moving Average")]
public class HullMA : IBacktestRunner
{
    private List<TickerData> _tickerData = new();
    
    bool positionOpen = false;
    private decimal buyPrice = 0;
    private decimal takeProfit = 0.05m; // % take profit
    private decimal stopLoss = 0.1m; // % stop loss
    
    private DateTime? timeDelay = null;
    private int timeDelayScore = 0;
    
    decimal SL = 0;
    decimal TP = 0;

    private decimal? prevCCI = null;
    
    public override DateTime FromDate { get; set; } = new DateTime(2020, 01, 01);
    public override DateTime ToDate { get; set; } = new DateTime(2023, 01, 01);
    public override BarTimeFrame TimeFrame { get; set; } = BarTimeFrame.Hour;

    public override async Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient) { }

     public string ExtraDetails()
     {
         try
         {
             return $"""
                     hit stop loss: {SL}
                     hit take profit: {TP}
                     win % : {(TP / (SL + TP)) * 100:F}
                     """;
         }
         catch (Exception e)
         {
             return "";
         }
         
     }

    public override void OnTick(BacktestState state)
    {
        var candle = state.GetCurrentCandle();
        _tickerData.Add(candle.CandleToTicker());
        if (_tickerData.Count < 75)
        {
            return;
        }
        _tickerData.RemoveAt(0);

        
        if (positionOpen)
        {
            if (candle.Close >= buyPrice * (1 + takeProfit + 0.025m))
            {
                state.Trade.Spot.Sell();
                positionOpen = false;
                
                state.AddLogEntry($"Hit take profit of {buyPrice * (1 + takeProfit)} from {buyPrice}");
                TP++;
                buyPrice = 0;
                return;
            } 
            
            if (candle.Close < buyPrice - (buyPrice * stopLoss))
            {
                state.Trade.Spot.Sell();
                positionOpen = false;
                state.AddLogEntry($"Hit stop loss of {buyPrice - (buyPrice * stopLoss)} from {buyPrice}");
                SL++;
                buyPrice = 0;
                return;
            }
        
            
        }
        
        
        
        var stockData = new StockData(_tickerData, InputName.Midpoint);
        var fastHma = stockData.CalculateHullMovingAverage(MovingAvgType.HullMovingAverage, 12).LatestValue("Hma");
        stockData.Clear();
        var slowHma = stockData.CalculateHullMovingAverage(MovingAvgType.HullMovingAverage, 72).LatestValue("Hma");
        stockData.Clear();
        var cci = (decimal) stockData.CalculateCommodityChannelIndex(InputName.Midpoint).LatestValue("Cci");
        if (prevCCI == null)
        {
            prevCCI = cci;
            return;
        }
        

        var turnUp = cci< -100 && cci > prevCCI;
        var turnDown = cci > 100 && cci < prevCCI;
    
        
        if (!positionOpen && fastHma > slowHma && turnUp)
        {
            state.Trade.Spot.Buy(AmountType.Percentage, 8);
            positionOpen = true;
            buyPrice = state.GetLastSpotTrade().QuotePrice;
        }

        if (positionOpen && fastHma < slowHma && turnDown)
        {
            state.Trade.Spot.Sell();
            positionOpen = false;
        }

        
        prevCCI = cci;
    }
}
