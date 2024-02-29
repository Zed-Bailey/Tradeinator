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


    public Task<CreateOrderResponse> OpenLongPosition(string accountId, string instrumentName, double units, int stopLossPips)
    {
        
        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = instrumentName,
            Units = double.Floor(units).ToString(),
            StopLossOnFill = new
            {
                // has to be a string other will get the STOP_LOSS_ON_FILL_PRICE_PRECISION_EXCEEDED error
                // see -> https://developer.oanda.com/rest-live-v20/troubleshooting-errors/#PRECISION_EXCEEDED
                distance = stopLossPips.ToString() 
            },
            Type = "MARKET"
        });
        
        return _oandaApiConnection.OrderApi.CreateOrderAsync(accountId, orderRequest);
    }
    
    


    public Task<CreateOrderResponse> OpenShortPosition(string account, string instrumentName, double units, int stopLossPips)
    {
        var orderRequest = new CreateOrderRequest(new
        {
            Instrument = instrumentName,
            Units = double.Floor(units * -1).ToString(),
            StopLossOnFill = new
            {
                // has to be a string other will get the STOP_LOSS_ON_FILL_PRICE_PRECISION_EXCEEDED error
                // see -> https://developer.oanda.com/rest-live-v20/troubleshooting-errors/#PRECISION_EXCEEDED
                distance = stopLossPips.ToString() 
            },
            Type = "MARKET"
        });
        
        return _oandaApiConnection.OrderApi.CreateOrderAsync(account, orderRequest);
    }
}