using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Extensions;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;

namespace Tradeinator.DataIngestion.Forex;

public class OandaConnection
{
    private string _apiKey;
    private IOandaApiConnection _connection;
    public OandaConnection(string apiKey)
    {
        _apiKey = apiKey;
         _connection = new OandaApiConnectionFactory().CreateConnection(
                OandaConnectionType.FxPractice,
                _apiKey
            );
    }


    public async Task<Bar?> GetLatestData(string symbol)
    {
        if (!Enum.TryParse<InstrumentName>(symbol, out var iName)) return null;
        
        var candle = await _connection.InstrumentApi.GetInstrumentCandlesAsync(
            iName,
            DateTimeFormat.RFC3339,
            smooth: true,
            granularity: CandlestickGranularity.M30,
            from: DateTime.Now.AddHours(-2).ToOandaDateTime(DateTimeFormat.RFC3339)
            // to: DateTime.Now.ToString()
        );
        
        if (candle is null) return null;

        var latest = candle.Candles.Last();
        if (latest is null) return null;
        
        return new Bar()
        {
            Open = latest.Mid.O.ToDecimal(),
            High = latest.Mid.H.ToDecimal(),
            Low = latest.Mid.L.ToDecimal(),
            Close = latest.Mid.C.ToDecimal(),
            Volume = latest.Volume,
            TimeUtc = DateTime.Parse(latest.Time).ToUniversalTime()
        };
    }
}