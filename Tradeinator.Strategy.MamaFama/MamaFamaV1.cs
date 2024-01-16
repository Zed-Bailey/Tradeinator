using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.MamaFama;

public class MamaFamaV1 : StrategyBase
{
    private string _accountId;
    private string _apiToken;
    
    private List<TickerData> _data = new();

    private IOandaApiConnection _oandaApiConnection;

    private bool _isLong = false;
    private bool _tradeOpen = false;
    private double stopLoss = 0;

    private string transactionId = "";
    
    public MamaFamaV1(string accountId, string apiToken)
    {
        _accountId = accountId;
        _apiToken = apiToken;
        _oandaApiConnection = new OandaApiConnection(OandaConnectionType.FxPractice, _apiToken);
    }

    public async Task Init()
    {
        var data = await _oandaApiConnection.InstrumentApi.GetInstrumentCandlesAsync(
            InstrumentName.AUD_CHF,
            DateTimeFormat.RFC3339,
            granularity: CandlestickGranularity.M30,
            smooth: true,
            count: 200
        );

        var candles = data?.Candles;
        if (candles is null) throw new Exception("candle data was not loaded in");

        foreach (var candle in candles)
        {
            var t = new TickerData
            {
                Open = candle.Mid.O,
                High = candle.Mid.H,
                Low = candle.Mid.L,
                Close = candle.Mid.C,
                Volume = candle.Volume,
                Date = DateTime.Parse(candle.Time).ToUniversalTime()
            };
            
            _data.Add(t);
        }
    }

    public override async void NewBar(Bar bar)
    {
        _data.Add(bar.ToTickerData());
        if(_data.Count < 200) return;
        
        _data.RemoveAt(0);

        // var account = await _oandaApiConnection.PositionApi.GetOpenPositionsAsync(_accountId);


        var stockData = new StockData(_data);
        
        var adx = stockData.CalculateAverageDirectionalIndex().LatestValue("Adx");
        var isTrending = adx > 25;
        
        stockData.Clear();
        
        var ehlersMotherAMA = stockData.CalculateEhlersMotherOfAdaptiveMovingAverages(
            fastAlpha: isTrending ? 0.5 : 0.25,
            slowAlpha: isTrending ? 0.05 : 0.04
        );

        var famaMamaCrossOver = ehlersMotherAMA.CrossOver("Mama", "Fama");
        var famaMamaCrossUnder = ehlersMotherAMA.CrossUnder("Mama", "Fama");

        stockData.Clear();
        
        // borrow less in a ranging market
        var borrowAmount = isTrending ? 2.0m : 1.25m;
        
        var adaptiveTs = (decimal) stockData.CalculateAdaptiveTrailingStop().LatestValue("Ts");
        // mama has crossed from below
        if (famaMamaCrossOver)
        {
            if (_tradeOpen && !_isLong)
            {
                // state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
            } 
            if (!_tradeOpen)
            {
                // tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.BaseBalance * borrowAmount);
                _tradeOpen = true;
                _isLong = true;
                // sl = adaptiveTs;
            }

            return;
        }
        // mama has crossed under
        if (famaMamaCrossUnder)
        {
            if (_tradeOpen && _isLong)
            {
                // state.Trade.Margin.ClosePosition(tradeId);
                _tradeOpen = false;
            } 
            if (!_tradeOpen)
            {
                // tradeId = state.Trade.Margin.Short(AmountType.Absolute, state.BaseBalance * borrowAmount);
                _tradeOpen = true;
                _isLong = false;
                // sl = adaptiveTs;
            }

            return;

        }
        
    }

    public override void Dispose()
    {
        // close trades?
    }

    

}