using Alpaca.Markets;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public abstract class IBacktestRunner
{
    public abstract DateTime FromDate { get; set; }
    public abstract DateTime ToDate { get; set; }
    public abstract BarTimeFrame TimeFrame { get; set; } 
        
    public abstract Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient);
    public abstract void OnTick(BacktestState state);

    public virtual string ExtraDetails() => "";
}