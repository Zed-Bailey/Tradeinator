using Alpaca.Markets;

namespace Tradeinator.Strategy.Shared;

/// <summary>
/// an interface to implement custom order submission based upon if the applicatio
/// </summary>
public interface ITradeManager
{
    Task SubmitOrder(
        string symbol, int quantity, decimal price, OrderSide side,
        decimal? stopLoss = null, decimal? takeProfit = null
    );
    
    object GetUnderlyingClient();
}