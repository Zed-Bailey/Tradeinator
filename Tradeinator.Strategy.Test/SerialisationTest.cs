using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.Test;


public class SerialisationTest
{
    [Fact]
    public void SerialiseStrategy_ReturnsJson()
    {
        var expected = "[{\"Name\":\"Type\",\"PropertyName\":\"Type\",\"Value\":10,\"Type\":\"System.Int32\"}]";
        var strategy = new ExampleStrategy();
        var loader = new StrategyLoader();
        var actual = loader.SerialiseStrategy(strategy);
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LoadSimpleStrategy_ReturnsConfiguredStrategy()
    {
        var expected = "[{\"Name\":\"Type\",\"PropertyName\":\"Type\",\"Value\":10,\"Type\":\"System.Int32\"}]";
        var loader = new StrategyLoader();
        var actual = loader.LoadStrategy<ExampleStrategy>(expected);
        
        Assert.NotNull(actual);
        Assert.Equal(10, actual.Type);
    }
}