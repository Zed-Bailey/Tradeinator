using Alpaca.Markets;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public interface IBacktestRunner
{
    public abstract DateTime FromDate { get; set; }
    public abstract DateTime ToDate { get; set; }
    
    Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient);
    void OnTick(BacktestState state);
}