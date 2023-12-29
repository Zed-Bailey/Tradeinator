using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Backtester.Helpers;

namespace Tradeinator.Backtester.Strategies;

[BackTestStrategyMetadata("Falling cross strategy")]
public class FallingCrossMA : IBacktestRunner
{
    private List<TickerData> _tickerData = new();
    
    bool positionOpen = false;
    private decimal buyPrice = 0;
    private decimal takeProfit = 0.02m; // % take profit
    private decimal stopLoss = 0.08m; // % stop loss
    
    private DateTime? timeDelay = null;
    private int timeDelayScore = 0;
    
    decimal SL = 0;
    decimal TP = 0;
    
    
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

    public string ExtraDetails()
    {
        return $"""
                hit stop loss: {SL}
                hit take profit: {TP}
                win % : {(TP / (SL + TP)) * 100:F}
                """;
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


        if (positionOpen)
        {
            if (candle.Close >= buyPrice * (1 + takeProfit + 0.025m))
            {
                state.Trade.Spot.Sell();
                positionOpen = false;
                
                state.AddLogEntry($"Hit take profit of {buyPrice * (1 + takeProfit)} from {buyPrice}");
                TP++;
                buyPrice = 0;
            } 
            else if (candle.Close < buyPrice - (buyPrice * stopLoss))
            {
                state.Trade.Spot.Sell();
                positionOpen = false;
                state.AddLogEntry($"Hit stop loss of {buyPrice - (buyPrice * stopLoss)} from {buyPrice}");
                SL++;
                buyPrice = 0;
            }

            return;
        }
        
        if(timeDelay != null && candle.Time >= timeDelay)
        {
            state.AddLogEntry($"bscore: {timeDelayScore}");
            state.Trade.Spot.Buy(AmountType.Percentage, timeDelayScore);
                
            positionOpen = true;
            timeDelay = null;
            timeDelayScore = 0;
            buyPrice = state.GetLastSpotTrade().QuotePrice;
            return;
        }
        
        var bscore = 0;
        var stockData = new StockData(_tickerData);
        var sma = stockData.CalculateSimpleMovingAverage().LatestValue("Sma");
        stockData.Clear();
        var fallingRisingFilter = stockData.CalculateFallingRisingFilter(50).LatestValue("Frf");
        if (sma < fallingRisingFilter) bscore++;
        
        stockData.Clear();

        var vsi = stockData.CalculateVolatilitySwitchIndicator().LatestValue("Vsi");
        if (vsi == 0) bscore++;
        
        stockData.Clear();
        
        // var rsi = stockData.CalculateRelativeStrengthIndex().LatestValue("Rsi");
        // if (rsi <= 40) bscore++;
        // stockData.Clear();
        
        var smaShort = stockData.CalculateSimpleMovingAverage(5).LatestValue("Sma");
        stockData.Clear();
        var vwap = stockData.CalculateVolumeWeightedAveragePrice().LatestValue("Vwap");
        if (vwap <= smaShort) bscore++;

        
        stockData.Clear();
        
        var bollingerPercent = stockData.CalculateBollingerBandsPercentB(50).LatestValue("PctB");
        if (bollingerPercent < 0) bscore++;
        
        stockData.Clear();
        var trendTriggerFactor = stockData.CalculateTrendTriggerFactor().LatestValue("Ttf");
        if (trendTriggerFactor < -100) bscore++;
        
        
        if (bscore > 2 && !positionOpen)
        {
            if (timeDelay == null)
            {
                timeDelay = candle.Time.AddHours(1);
                timeDelayScore = bscore;
            }
        }
        

    }
}
