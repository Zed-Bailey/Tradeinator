using Alpaca.Markets;
using Tradeinator.Configuration;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.Shared;

public abstract class StrategyBase : IAsyncDisposable
{

    public event EventHandler<SystemMessageEventArgs>? SendMessageNotification;

    /// <summary>
    /// Used to invoke the event handler as a derived class cant directly access it
    /// see -> https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/how-to-raise-base-class-events-in-derived-classes
    /// </summary>
    /// <param name="e">the system message to send</param>
    protected virtual void OnSendMessage(SystemMessageEventArgs e)
    {
        SendMessageNotification?.Invoke(this, e);
    }
    
    public virtual Task Init(ConfigurationLoader configuration)
    {
        return Task.CompletedTask;
    }

    public abstract void NewBar(Bar bar);
    

    public abstract ValueTask DisposeAsync();
}