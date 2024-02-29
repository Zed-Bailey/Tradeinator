using System.Net;
using System.Net.Http.Json;
using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using MathNet.Numerics;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;

namespace Tradeinator.DataIngestion.Shared;

public class OandaConnection
{
    private string _apiKey;
    private IOandaApiConnection _connection;
    private HttpClient _sharedClient;
    
    public OandaConnection(string apiKey)
    {
        _apiKey = apiKey;
        _connection = new OandaApiConnectionFactory().CreateConnection(
                OandaConnectionType.FxPractice,
                _apiKey
            );
        _sharedClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api-fxpractice.oanda.com/v3/"),
                DefaultRequestHeaders =
                {
                    {"Authorization", "Bearer " + apiKey},
                }
            };
    }




    private record CandlesResponse(
        string instrument,
        string granularity,
        IEnumerable<Candlestick> candles
    );
    
    
    /// <summary>
    /// Gets the latest bar for the instrument
    /// </summary>
    /// <param name="symbol">instrument symbol eg. AUD/USD </param>
    /// <param name="granularity">granularity of data</param>
    /// <returns>The latest bar. null if symbol could not be parsed or the latest candle is null</returns>
    public async Task<Bar?> GetLatestData(string symbol, string granularity = "M30", int count = 1)
    {
        var cleanedSymbol = symbol.Replace('/', '_');
        
        var candlesResponse = await _sharedClient.GetAsync(
            $"instruments/{cleanedSymbol}/candles?count={count}&price=M&granularity={granularity}");


        if (candlesResponse.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        
        var candles = await candlesResponse.Content.ReadFromJsonAsync<CandlesResponse?>();
        var latest = candles?.candles.LastOrDefault();
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