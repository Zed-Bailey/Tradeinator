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


    /// <summary>
    /// Gets the latest bar for the instrument
    /// </summary>
    /// <param name="symbol">instrument symbol eg. AUD/USD </param>
    /// <param name="granularity">granularity of data</param>
    /// <returns>The latest bar. null if symbol could not be parsed or the latest candle is null</returns>
    public async Task<Bar?> GetLatestData(string symbol, CandlestickGranularity granularity = CandlestickGranularity.M30)
    {
        // converts AUD/CHF => AUD_CHF which matches enum naming so it can be parsed
        var cleaned = symbol.Replace("/", "_");
        if (!Enum.TryParse<InstrumentName>(cleaned, out var iName)) return null;

        var  candle = await _connection.GetInstrument(iName)
            .GetLastNCandlesAsync(granularity, 1);
        
        // var candle = await _connection.InstrumentApi.GetInstrumentCandlesAsync(
        //     iName,
        //     DateTimeFormat.RFC3339,
        //     smooth: true,
        //     granularity: CandlestickGranularity.M30,
        //     from: DateTime.Now.AddHours(-2).ToOandaDateTime(DateTimeFormat.RFC3339)
        // );

        var latest = candle.LastOrDefault();
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