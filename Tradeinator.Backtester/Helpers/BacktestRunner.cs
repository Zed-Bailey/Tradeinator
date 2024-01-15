using Alpaca.Markets;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public abstract class BacktestRunner
{
    
    public virtual DateTime FromDate { get; set; } = new DateTime(2020, 01, 01);
    public virtual DateTime ToDate { get; set; } = DateTime.Now;
    public virtual BarTimeFrame TimeFrame { get; set; } = BarTimeFrame.Hour;
        
    public abstract Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient);
    public abstract void OnTick(BacktestState state);

    /// <summary>
    /// Close any positions on backtest finish
    /// </summary>
    public virtual void OnFinish(BacktestState state)
    { }

    public virtual string ExtraDetails() => "";
}