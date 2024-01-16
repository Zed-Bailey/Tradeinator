using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;

namespace Tradeinator.Strategy.MamaFama;

public class OandaTradeManager
{
    private string _apiToken;
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

    public async Task OpenLongPosition(string account)
    {
        _oandaApiConnection.OrderApi.CreateOrder()
    }
}