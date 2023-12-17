using Alpaca.Markets;
using SimpleBacktestLib;

namespace Tradeinator.Backtester;

public interface IBacktestRunner
{
    Task InitStrategy(string symbol, DateTime startDate, IAlpacaCryptoDataClient dataClient);
    void OnTick(BacktestState state);
}