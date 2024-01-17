using Alpaca.Markets;
using Serilog.Core;
using Tradeinator.Shared;

namespace Tradeinator.DataIngestion.Shared;

public class CryptoSubscriptionManager : AlpacaSubscriptionManager
{
    private IAlpacaCryptoStreamingClient _client;

    public CryptoSubscriptionManager(Logger logger, PublisherExchange exchange, IAlpacaCryptoStreamingClient client, string directoryPath, string symbolsFileName) : base(logger, exchange, directoryPath, symbolsFileName)
    {
        _client = client;
    }


    public override  ValueTask Subscribe(params IAlpacaDataSubscription<IBar>[] subscriptions)
    {
        return _client.SubscribeAsync(subscriptions);
    }

    public override ValueTask UnSubscribe(params IAlpacaDataSubscription<IBar>[] subscriptions)
    {
        return _client.UnsubscribeAsync(subscriptions);
    }

    public override IAlpacaDataSubscription<IBar> GetSubscription(string symbol)
    {
        return _client.GetMinuteBarSubscription(symbol);
    }
}