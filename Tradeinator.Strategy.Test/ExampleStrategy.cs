using Tradeinator.Shared.Attributes;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.Test;

public class ExampleStrategy: StrategyBase
{

    [SerialisableParameter("Type", )]
    public int Type { get; set; }
    
    
    
    
    
    public override void NewBar(Bar bar) { }

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}