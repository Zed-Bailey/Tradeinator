using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using Serilog;
using Serilog.Core;
using Tradeinator.Shared.EventArgs;
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

    private string _transactionId = "";
    
    private OandaTradeManager _tradeManager;
    private Logger _logger;
    
    public MamaFamaV1(string accountId, string apiToken)
    {
        _accountId = accountId;
        _apiToken = apiToken;
        _oandaApiConnection = new OandaApiConnection(OandaConnectionType.FxPractice, _apiToken);
        _tradeManager = new OandaTradeManager(_apiToken);
        
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"{nameof(MamaFamaV3)}.log")
            .CreateLogger();
    }

    public override async Task Init()
    {
        var candles = await _oandaApiConnection
            .GetInstrument(InstrumentName.AUD_CHF)
            .GetLastNCandlesAsync(CandlestickGranularity.M30, 200);

        // var data = await _oandaApiConnection.InstrumentApi.GetInstrumentCandlesAsync(
        //     InstrumentName.AUD_CHF,
        //     DateTimeFormat.RFC3339,
        //     granularity: CandlestickGranularity.M30,
        //     smooth: true,
        //     count: 200
        // );
        //
        // var candles = data?.Candles;
        var candlesticks = candles.ToArray();
        if (candles == null || !candlesticks.Any()) throw new Exception("candle data was not loaded in");

        foreach (var candle in candlesticks.ToList())
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
    
    private async void PositionNotOpenAnymore(string id)
    {
        _logger.Information("trade {Id} stop loss triggered", id);
        var trade = await _oandaApiConnection.TradeApi.GetTradeAsync(_accountId, id);
        if (trade.Trade.StopLossOrder.State is OrderState.TRIGGERED or OrderState.FILLED)
        {
            OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateStopLossTriggeredMessage(nameof(MamaFamaV1), trade.Trade)));
        }
    }

    public override async void NewBar(Bar bar)
    {
        _data.Add(bar.ToTickerData());
        if(_data.Count < 200) return;
        
        _data.RemoveAt(0);

        var account = await _oandaApiConnection.GetAccount(_accountId).GetSummaryAsync();
        
        
        
        if (_tradeOpen)
        {
            var openPositions = await _oandaApiConnection.PositionApi.GetOpenPositionsAsync(_accountId);
            // stop loss was triggered?
            if (!openPositions.Positions.Any())
            {
                _tradeOpen = false;
                PositionNotOpenAnymore(_transactionId);
            }
        }
        
        

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
        var borrowAmount = isTrending ? 2.0 : 1.25;
        
        var adaptiveTs = stockData.CalculateAdaptiveTrailingStop().LatestValue("Ts");
        // mama has crossed from below
        if (famaMamaCrossOver)
        {
            if (_tradeOpen && !_isLong)
            {
                var pos = await _tradeManager.ClosePosition(_accountId, false);
                _logger.Information("Fama crossed over Mama, closed short position, {Id}", _transactionId);
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateClosePositionMessage(nameof(MamaFamaV1),pos, isLongPosition: false)));
                _tradeOpen = false;
            }
            if (!_tradeOpen)
            {
                // tradeId = state.Trade.Margin.Long(AmountType.Absolute, state.BaseBalance * borrowAmount);
                var pos = await _tradeManager.OpenLongPosition(_accountId, account.Balance * borrowAmount, adaptiveTs);
                if (string.IsNullOrEmpty(pos.ErrorCode))
                {
                    _transactionId = pos.OrderFillTransaction.TradeOpened.TradeID.ToString();
                    _tradeOpen = true;
                    _isLong = true;
                    _logger.Information("Fama crossed over Mama, opened long position, {Id}", _transactionId);
                }

                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateOpenOrderMessage(nameof(MamaFamaV1),pos)));

            }

            return;
        }
        
        // mama has crossed under
        if (famaMamaCrossUnder)
        {
            if (_tradeOpen && _isLong)
            {
                var closePos = await _tradeManager.ClosePosition(_accountId);
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateClosePositionMessage(nameof(MamaFamaV1),closePos, isLongPosition: true)));
                _tradeOpen = false;
                _logger.Information("Fama crossed under Mama, closed long position {Id}", _transactionId);
            }
            
            if (!_tradeOpen)
            {
                var pos = await _tradeManager.OpenShortPosition(_accountId, 50, adaptiveTs);
                if (string.IsNullOrEmpty(pos.ErrorCode))
                {
                    _tradeOpen = true;
                    _transactionId = pos.OrderFillTransaction.TradeOpened.TradeID.ToString();
                    _isLong = false;
                    _logger.Information("Fama crossed under Mama, opened short position {Id}", _transactionId);

                }

                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateOpenOrderMessage(nameof(MamaFamaV1),pos)));
            }

            return;
        }
        
    }

    public override async ValueTask DisposeAsync()
    {
        _logger.Information("stopping strategy");
        // add 5 pip take profit to the open trade
        if (_tradeOpen)
        {
            var trade = await _oandaApiConnection.TradeApi.GetTradeAsync(_accountId, _transactionId);
            // if position has profit, close
            if (trade.Trade.UnrealizedPL > 0)
            {
                _logger.Information("Open position {Id} had unrealized P/L, closing now", _transactionId);
                await _tradeManager.ClosePosition(_accountId, _isLong);
                return;
            }
            
            _logger.Information("Setting take profit for trade");
            // add a take profit order to the position of 5 pips
            var opened = trade.Trade.Price;
            double takeProfitPrice;
            if (trade.Trade.InitialUnits < 0)
            {
                // short position
                takeProfitPrice = opened - (5 * 0.0001);
            }
            else
            {
                takeProfitPrice = opened + (5 * 0.0001);
            }

            await _oandaApiConnection.TradeApi.SetTradeOrdersAsync(_accountId, _transactionId,
                new DependentTradeOrdersRequest(
                    takeProfit: new TakeProfitDetails(
                        price: takeProfitPrice
                    )
                )
            );
        }

        await _logger.DisposeAsync();
    }


    

}