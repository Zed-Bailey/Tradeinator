using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.Test;


public class SerialisationTest
{
    [Fact]
    public void SerialiseStrategy_ReturnsJsonObject()
    {
        var expected = "[{\"DescriptiveName\":\"SomeValue\",\"PropertyName\":\"SomeValue\",\"Value\":10,\"Type\":\"System.Int32\"}]";
        var strategy = new ExampleStrategy();
        var actual = StrategyLoader.SerialiseStrategy(strategy);
        
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LoadSerialisedStrategy_ReturnsConfiguredStrategy()
    {
        var expected = "[{\"DescriptiveName\":\"SomeValue\",\"PropertyName\":\"SomeValue\",\"Value\":10,\"Type\":\"System.Int32\"}]";
        var actual = StrategyLoader.LoadStrategy<ExampleStrategy>(expected);
        
        Assert.NotNull(actual);
        Assert.Equal(10, actual.SomeValue);
    }
}