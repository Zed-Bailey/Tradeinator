namespace Tradeinator.Backtester.Helpers;

[AttributeUsage(AttributeTargets.Class)]
public class BackTestStrategyMetadata : Attribute
{
    public string StrategyName { get; init; }

    public double StartingBalance { get; set; } = 5000;
    
    public BackTestStrategyMetadata(string strategyName)
    {
        this.StrategyName = strategyName;
    }
        
}