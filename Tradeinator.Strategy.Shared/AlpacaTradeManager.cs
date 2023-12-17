using Alpaca.Markets;

namespace Tradeinator.Strategy.Shared;

public class AlpacaTradeManager: ITradeManager, IDisposable
{
    private IAlpacaTradingClient _alpacaTradingClient;
    
    public AlpacaTradeManager(string key, string secret)
    {
        _alpacaTradingClient = Environments.Paper.GetAlpacaTradingClient(new SecretKey(key, secret));
    }
    
    public async Task SubmitOrder(string symbol, int quantity, decimal price, OrderSide side)
    {
        // empty order
        if (quantity == 0) return;
        try
        {
            // var order = await _alpacaTradingClient.PostOrderAsync(
            //     side.Limit(symbol, quantity, price));

            
        }
        catch (Exception e)
        {
            Console.WriteLine("Warning: " + e.Message); //-V5621
        }
         
    }

    public object GetUnderlyingClient()
    {
        return _alpacaTradingClient;
    }

    public void Dispose()
    {
        _alpacaTradingClient?.Dispose();
    }
}