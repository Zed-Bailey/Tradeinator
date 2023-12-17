using Alpaca.Markets;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.Shared;

public abstract class Strategy : IDisposable
{
    public delegate void OnLogEntryCallBack(string message);

    public event OnLogEntryCallBack OnLogEntry;
    
    public virtual Task Init()
    {
        return Task.CompletedTask;
    }

    public virtual void NewBar(Bar bar) { }

    protected virtual void Log(string message)
    {
        OnLogEntry?.Invoke(message);
    }

    public virtual Task SubmitOrder(int quantity, decimal price, OrderSide side)
    {
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        
    }

}