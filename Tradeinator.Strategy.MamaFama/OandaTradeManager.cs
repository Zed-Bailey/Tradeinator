using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using GeriRemenyi.Oanda.V20.Sdk.Trade;
using MathNet.Numerics;

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

    public Task<CreateOrderResponse> OpenLongPosition(string accountId, double units, double stopLossPrice)
    {
        
        
        var order = new MarketOrder(
            InstrumentName.AUD_CHF,
            units
        );
        
        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = order.Instrument,
            Units = double.Floor(order.Units).ToString(),
            StopLossOnFill = new
            {
                // has to be a string other will get the STOP_LOSS_ON_FILL_PRICE_PRECISION_EXCEEDED error
                // see -> https://developer.oanda.com/rest-live-v20/troubleshooting-errors/#PRECISION_EXCEEDED
                price = double.Round(stopLossPrice, 5).ToString() 
            },
            Type = "MARKET"
        });
        
        return _oandaApiConnection.OrderApi.CreateOrderAsync(accountId, orderRequest);
    }
    
    
    public Task<CreateOrderResponse> OpenShortPosition(string account, double units, double stopLossPrice)
    {


        var order = new MarketOrder(
            InstrumentName.AUD_CHF,
            units * -1 // short needs to be negative
        );

        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = order.Instrument,
            Units = double.Floor(order.Units).ToString(),
            StopLossOnFill = new
            {
                // has to be a string other will get the STOP_LOSS_ON_FILL_PRICE_PRECISION_EXCEEDED error
                // see -> https://developer.oanda.com/rest-live-v20/troubleshooting-errors/#PRECISION_EXCEEDED
                price = double.Round(stopLossPrice, 5).ToString() 
            },
            Type = "MARKET"
        });

        return _oandaApiConnection.OrderApi.CreateOrderAsync(account, orderRequest);
    }
}