using Tradeinator.Shared;

// implementation of the mean reversion strategy from
// https://github.com/alpacahq/alpaca-trade-api-csharp/blob/develop/UsageExamples/MeanReversionPaperOnly.cs

const string host = "localhost";
const string exchangeName = "test_exchange";

using var exchange = new ReceiverExchange(host, exchangeName, "bar.ETH/USD");

exchange.ConsumerOnReceive += (sender, eventArgs) =>
{

};


await exchange.StartConsuming();