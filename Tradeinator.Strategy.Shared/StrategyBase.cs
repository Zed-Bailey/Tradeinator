using Alpaca.Markets;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.Shared;

public abstract class StrategyBase : IDisposable
{
    public delegate void OnLogEntryCallBack(string message);
    public event OnLogEntryCallBack OnLogEntry;
    
    public virtual Task Init()
    {
        return Task.CompletedTask;
    }

    public abstract void NewBar(Bar bar);

    protected virtual void Log(string message)
    {
        OnLogEntry?.Invoke(message);
    }
    

    public abstract void Dispose();

}