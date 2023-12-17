using Alpaca.Markets;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using SimpleBacktestLib;
using Tradeinator.Strategy.MeanReversion;

namespace Tradeinator.Backtester;

public class MeanReversionBacktest : IBacktestRunner
{
    private List<decimal> _closingPrices = new();
    private bool lastTradeOpen = false;
    private int? lastTradeId = null;

    private bool shorted = false;
    private decimal atPrice;

    private StockData _stockData;
    
    

    public async Task InitStrategy(string symbol, DateTime startDate, IAlpacaCryptoDataClient dataClient)
    { }

    public void OnTick(BacktestState state)
    {
        var currCandle = state.GetCurrentCandle();
        _stockData.TickerDataList.Add(new TickerData
        {
            Open = (double) currCandle.Open,
            High = (double) currCandle.High,
            Low = (double) currCandle.Low,
            Close = (double) currCandle.Close,
            Volume = (double) currCandle.Volume,
            Date = currCandle.Time
        });
        
        if (_stockData.TickerDataList.Count < 90)
        {
            return;
        }

        var ninety = _stockData.CalculateSimpleMovingAverage(90);
        _stockData.Clear();
        var thirty = _stockData.CalculateSimpleMovingAverage(30);
        _stockData.Clear();

        // if (thirty.OutputValues["SMA30"] < ninety.OutputValues["SMA90"])
        // {
        //     
        // }


    }
}

// _closingPrices.RemoveAt(0);
// var avg = _closingPrices.Average();
// var diff = avg - state.GetCurrentCandle().Close;
//
//
// if (lastTradeOpen)
// {
//     if (shorted)
//     {
//         if (atPrice - state.GetCurrentCandle().Close > 0)
//         {
//             state.Trade.Margin.ClosePosition(lastTradeId.Value);
//             lastTradeOpen = false;
//         }
//         else
//         {
//             return;
//         }
//     }
//     else
//     {
//         if (state.GetCurrentCandle().Close - atPrice > 0)
//         {
//             state.Trade.Margin.ClosePosition(lastTradeId.Value);
//             lastTradeOpen = false;
//         }
//         else
//         {
//             return;
//         }
//     }
// }
//
// state.AddLogEntry($"date: {state.GetCurrentCandle().Time} || avg: {avg}, diff: {diff}");
// // we are above the mean
// if (diff > 100)
// {
//     lastTradeId = state.Trade.Margin.Short();
//     lastTradeOpen = true;
//     shorted = true;
// }
// else if(diff < -100)
// {
//     lastTradeId = state.Trade.Margin.Long();
//     lastTradeOpen = true;
//     shorted = false;
// }