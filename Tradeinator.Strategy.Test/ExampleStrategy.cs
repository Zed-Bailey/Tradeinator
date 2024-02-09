using Tradeinator.Shared.Attributes;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.Test;

public class ExampleStrategy: StrategyBase
{

    [SerialisableParameter("SomeValue")]
    public int SomeValue { get; set; } = 10;

    public int NoSerialisationAttribute { get; set; }
    
    
    
    public override void NewBar(Bar bar) { }

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}