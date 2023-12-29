namespace Tradeinator.Backtester.Helpers;

public record AvailableStrategy(IBacktestRunner backtestRunner, BackTestStrategyMetadata attribute);