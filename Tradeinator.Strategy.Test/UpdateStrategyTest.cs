using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.Test;

public class UpdateStrategyTest
{

    [Fact]
    public void NewConfig_UpdatesExistingStrategy()
    {
        var strategy = new ExampleStrategy();
        
        Assert.Equal(10, strategy.SomeValue);
        var loader = new StrategyLoader();
        
        var newStrategy = loader.SerialiseStrategy(new ExampleStrategy() { SomeValue = 100 });
        
        loader.UpdateStrategyProperties(strategy, newStrategy);
        
        Assert.Equal(100, strategy.SomeValue);
        
        
    }
}