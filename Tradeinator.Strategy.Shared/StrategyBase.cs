using Alpaca.Markets;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.Shared;

public abstract class StrategyBase : IDisposable
{

    public event EventHandler<SystemMessageEventArgs>? SendMessageNotification;
    
    public virtual Task Init()
    {
        return Task.CompletedTask;
    }

    public abstract void NewBar(Bar bar);
    
    

    public abstract void Dispose();

}