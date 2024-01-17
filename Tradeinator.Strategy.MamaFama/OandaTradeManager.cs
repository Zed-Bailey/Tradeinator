using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using GeriRemenyi.Oanda.V20.Sdk.Trade;

namespace Tradeinator.Strategy.MamaFama;

public class OandaTradeManager
{
    private readonly string _apiToken;
    private IOandaApiConnection _oandaApiConnection;
    
    
    public OandaTradeManager( string apiToken)
    {
        _apiToken = apiToken;
        _oandaApiConnection = new OandaApiConnection(OandaConnectionType.FxPractice, _apiToken);
    }


    public async Task<ClosePositionResponse> ClosePosition(string account, bool longPosition = true)
    {
        var closeRequest = longPosition
            ? new ClosePositionRequest
            {
                LongUnits = "ALL"
            }
            : new ClosePositionRequest()
            {
                ShortUnits = "ALL"
            };
        
        var res = await _oandaApiConnection.PositionApi.ClosePositionAsync(
            account,
            InstrumentName.AUD_CHF,
            closeRequest
        );

        return res;
    }

    public async Task<CreateOrderResponse> OpenLongPosition(string account, double units, double stopLossPrice)
    {
        var stoploss = new StopLossDetails(
                price: stopLossPrice
        );

        var order = new MarketOrder(
            InstrumentName.AUD_CHF,
            units,
            stopLossOnFill: stoploss
        );

        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = order.Instrument,
            Units = order.Units,
            StopLossOnFill = order.StopLossOnFill,
            Type = "MARKET"
        });

        return await _oandaApiConnection.OrderApi.CreateOrderAsync(account, orderRequest);
    }
    
    
    public async Task<CreateOrderResponse> OpenShortPosition(string account, double units, double stopLossPrice)
    {
        var stoploss = new StopLossDetails(
            price: stopLossPrice
        );

        var order = new MarketOrder(
            InstrumentName.AUD_CHF,
            units * -1, // short needs to be negative
            stopLossOnFill: stoploss
        );

        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = order.Instrument,
            Units = order.Units,
            StopLossOnFill = order.StopLossOnFill,
            Type = "MARKET"
        });

        return await _oandaApiConnection.OrderApi.CreateOrderAsync(account, orderRequest);
    }
}