using SimpleBacktestLib;

namespace Tradeinator.Backtester.Helpers;

public class SimpleBackTest
{
    public static Task<BacktestResult> RunSimpleBacktest(BacktestBuilder builder, BacktestRunner backtest, int lastCandleIndex)
    {
        builder = builder
            .OnTick(state =>
            {
                backtest.OnTick(state);
            })
            .PostTick(e =>
            {
                if (e.CurrentCandleIndex == lastCandleIndex)
                {
                    backtest.OnFinish(e);
                }
                
            })
            .OnLogEntry((entry, _) =>
            {
                Console.WriteLine(entry.ToString());
            });
                
       
        
        return builder.RunAsync();;
    }
}