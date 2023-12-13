namespace Tradeinator.Shared.Models;

public class Bar
{
    
    public DateTime TimeUtc { get; }

    public decimal Open { get; }
   
    public decimal High { get; }

    public decimal Low { get; }

    public decimal Close { get; }

    public decimal Volume { get; }
    
    /// <summary>
    /// Volume Weighted Average Price
    /// </summary>
    public decimal Vwap { get; }
    
    public int TradeCount { get; }
    
}