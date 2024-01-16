using OoplesFinance.StockIndicators.Models;

namespace Tradeinator.Shared.Extensions;

public static class StockDataExtensions
{

    /// <summary>
    /// Returns the last 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static double LatestValue(this StockData data, string name)
    {
        return data.OutputValues[name].LastOrDefault();
    }
    
    /// <summary>
    /// checks if series 1 has crossed over series 2
    /// </summary>
    /// <param name="series1">first series key</param>
    /// <param name="series2">second series key</param>
    /// <returns>returns true if the current series 1 value is greater then series 2 and the previous series 1 value is less then series 2</returns>
    public static bool CrossOver(this StockData sd, string series1, string series2)
    {
        var s1Series = sd.OutputValues[series1];
        var s2Series = sd.OutputValues[series2];

        // not enough data
        if (s1Series.Count < 2 || s2Series.Count < 2) return false;
        
        var s1Prev = s1Series[^2];
        var s2Prev = s2Series[^2];

        var s1 = s1Series[^1];
        var s2 = s2Series[^1];

        return s1 > s2 && s1Prev < s2Prev;
    }
    
    /// <summary>
    /// checks if series 1 has crossed over series 2
    /// </summary>
    /// <param name="series1">first series key</param>
    /// <param name="series2">second value</param>
    /// <returns>returns true if the current series 1 value is greater then series 2 and the previous series 1 value is less then series 2</returns>
    public static bool CrossOver(this StockData sd, string series1, double series2)
    {
        var s1Series = sd.OutputValues[series1];
        
        // not enough data
        if (s1Series.Count < 2) return false;
        
        var s1Prev = s1Series[^2];
        
        var s1 = s1Series[^1];
        
        return s1 > series2 && s1Prev < series2;
    }
    
    /// <summary>
    /// checks if series 1 has crossed under series 2
    /// </summary>
    /// <param name="series1">first series key</param>
    /// <param name="series2">second series</param>
    /// <returns>returns true if the current series 1 value is less then series 2 and the previous series 1 value is greater then the previous series 2 value</returns>
    public static bool CrossUnder(this StockData sd, string series1, double series2)
    {
        var s1Series = sd.OutputValues[series1];
        

        // not enough data
        if (s1Series.Count < 2) return false;
        
        var s1Prev = s1Series[^2];

        var s1 = s1Series[^1];

        return s1 < series2 && s1Prev > series2;
    }
    
    
    /// <summary>
    /// checks if series 1 has crossed under series 2
    /// </summary>
    /// <param name="series1">first series key</param>
    /// <param name="series2">second series key</param>
    /// <returns>returns true if the current series 1 value is less then series 2 and the previous series 1 value is greater then the previous series 2 value</returns>
    public static bool CrossUnder(this StockData sd, string series1, string series2)
    {
        var s1Series = sd.OutputValues[series1];
        var s2Series = sd.OutputValues[series2];

        // not enough data
        if (s1Series.Count < 2 || s2Series.Count < 2) return false;
        
        var s1Prev = s1Series[^2];
        var s2Prev = s2Series[^2];

        var s1 = s1Series[^1];
        var s2 = s2Series[^1];

        return s1 < s2 && s1Prev > s2Prev;
    }
}