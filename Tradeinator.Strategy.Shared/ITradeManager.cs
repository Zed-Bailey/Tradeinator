using Alpaca.Markets;

namespace Tradeinator.Strategy.Shared;

/// <summary>
/// an interface to implement custom order submission based upon if the applicatio
/// </summary>
public interface ITradeManager
{
    Task SubmitOrder(string symbol, int quantity, decimal price, OrderSide side);
    object GetUnderlyingClient();
}