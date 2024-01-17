using OoplesFinance.StockIndicators.Models;

namespace Tradeinator.Shared.Models;

public class Bar
{
    
    public DateTime TimeUtc { get; set; }

    public decimal Open { get; set; }
   
    public decimal High { get;set; }

    public decimal Low { get; set;}

    public decimal Close { get; set;}

    public decimal Volume { get; set;}
    
    /// <summary>
    /// Volume Weighted Average Price
    /// </summary>
    public decimal Vwap { get; }
    
    public int TradeCount { get; }

    public TickerData ToTickerData()
    {
        return new TickerData
        {
            Open = (double) Open,
            Close = (double) Close,
            High = (double) High,
            Low = (double) Low,
            Volume = (double) Volume,
            Date = TimeUtc 
        };
    }
    
}