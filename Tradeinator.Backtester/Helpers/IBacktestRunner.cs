using Alpaca.Markets;
using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public interface IBacktestRunner
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public BarTimeFrame TimeFrame { get; set; } 
        
    Task InitStrategy(string symbol, IAlpacaCryptoDataClient dataClient);
    void OnTick(BacktestState state);

    string ExtraDetails() => "";
}