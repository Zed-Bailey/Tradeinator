using System.Text.Json;
using Tradeinator.Shared.Attributes;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;
using Xunit.Abstractions;

namespace Tradeinator.Strategy.Test;


public class SerialisationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SerialisationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    
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