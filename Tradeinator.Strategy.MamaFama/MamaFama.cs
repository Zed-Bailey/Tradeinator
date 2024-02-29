using GeriRemenyi.Oanda.V20.Client.Model;
using GeriRemenyi.Oanda.V20.Sdk;
using GeriRemenyi.Oanda.V20.Sdk.Common.Types;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Helpers;
using OoplesFinance.StockIndicators.Models;
using Serilog;
using Serilog.Core;
using Tradeinator.Configuration;
using Tradeinator.Shared.Attributes;
using Tradeinator.Shared.EventArgs;
using Tradeinator.Shared.Extensions;
using Tradeinator.Shared.Models;
using Tradeinator.Strategy.Shared;

namespace Tradeinator.Strategy.MamaFama;

public class MamaFama : StrategyBase
{


    private List<TickerData> _data = new();

    private IOandaApiConnection _oandaApiConnection;

    private bool _isLong = false;
    private bool _tradeOpen = false;

    private string _transactionId = "";
    private double _accountBalance;
    
    private OandaTradeManager _tradeManager;
    private Logger _logger;
    
    //---------
    // Strategy Properties
    [SerialisableParameter]
    public double RrsiLevel { get; set; } = 50D;
    
    [SerialisableParameter]
    public bool UseSecondaryTrigger { get; set; } = false;
    
    [SerialisableParameter]
    public string StrategyVersion { get; set; }
    //---------
    
    [SerialisableParameter]
    public string AccountId { get; set; }

    [SerialisableParameter]
    public int StopLossInPips { get; set; } = 25;
    
    
    private string _apiToken;
    
    public MamaFama() { }
    

    
    public override async Task Init(ConfigurationLoader configuration)
    {
        _apiToken = configuration["OANDA_API_TOKEN"] ?? throw new ArgumentNullException("Oanda api token was null");
        
        _oandaApiConnection = new OandaApiConnection(OandaConnectionType.FxPractice, _apiToken);
        _tradeManager = new OandaTradeManager(_apiToken);
        
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"{StrategyVersion}.log")
            .CreateLogger();
        
        var candles = await _oandaApiConnection
            .GetInstrument(InstrumentName.AUD_CHF)
            .GetLastNCandlesAsync(CandlestickGranularity.M30, 200);
        
        var candlesticks = candles.ToArray();
        if (candles == null || !candlesticks.Any()) throw new Exception("candle data was not loaded in as it was null or empty");

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
        _logger.Information("loaded {Num} candles", _data.Count);
    }
    
    private async void PositionNotOpenAnymore(string id)
    {
        _logger.Information("stop loss triggered for trade {Id}", id);
        var trade = await _oandaApiConnection.TradeApi.GetTradeAsync(AccountId, id);
        if (trade.Trade.StopLossOrder.State is OrderState.TRIGGERED or OrderState.FILLED)
        {
            OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateStopLossTriggeredMessage(StrategyVersion, trade.Trade)));
        }
    }

    private DateTime _prevBarTime = DateTime.MinValue;
    
    public override async void NewBar(Bar bar)
    {
        _logger.Information("New bar: {Bar}", bar);
        try
        {
            // handles the odd case where we get bars with the same time, price, etc..
            if (bar.TimeUtc > _prevBarTime)
            {
                _prevBarTime = bar.TimeUtc;
                await Execute(bar);
            }
            else
            {
                _logger.Warning("Received bar with same time as previous recorded time. prevBarTime={PrevBarTime}, newBarTime={NewBarTime}", _prevBarTime, bar.TimeUtc);
            }
            
        }
        catch (Exception e)
        {
            var msg = new SystemMessage
            {
                StrategyName = StrategyVersion,
                Priority = MessagePriority.Critical,
                Symbol = "AUD/CHF",
                Message =
                    $"An error occured while trying to execute the strategy\n{e.Message}\nsee log for full exception"
            };
            
            _logger.Fatal(e, "An exception occured while trying to run the strategy");
            OnSendMessage(new SystemMessageEventArgs(msg));
        }
        
    }

    
    
    private async Task Execute (Bar bar)
    {
        _data.Add(bar.ToTickerData());
        if(_data.Count < 200) return;
        
        _data.RemoveAt(0);

        var account = await _oandaApiConnection.GetAccount(AccountId).GetSummaryAsync();
        _accountBalance = account.Balance;
        
        
        if (_tradeOpen)
        {
            var openPositions = await _oandaApiConnection.PositionApi.GetOpenPositionsAsync(AccountId);
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
        var borrowMultipler = isTrending ? 2.0 : 1.25;
        var borrowAmount = _accountBalance * borrowMultipler;
        
        // var adaptiveTs = stockData.CalculateAdaptiveTrailingStop().LatestValue("Ts");
        // mama has crossed from below
        if (famaMamaCrossOver)
        {
            if (_tradeOpen && !_isLong)
            {
                _logger.Information("fama crossed over mama, closing short position {Id}", _transactionId);
                var pos = await _tradeManager.ClosePosition(AccountId, false);
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateClosePositionMessage(StrategyVersion,pos, isLongPosition: false)));
                _tradeOpen = false;
            }
            if (!_tradeOpen)
            {
                
                var pos = await _tradeManager.OpenLongPosition(AccountId, "AUD_CHF" , borrowAmount, StopLossInPips);

                if (string.IsNullOrEmpty(pos.ErrorCode))
                {
                    _transactionId = pos.OrderFillTransaction.TradeOpened.TradeID.ToString();
                    _logger.Information("fama crossed over mama, opened long position {Id}", _transactionId);
                    _tradeOpen = true;
                    _isLong = true;
                }

                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateOpenOrderMessage(StrategyVersion, pos)));

            }

            return;
        }
        
        // mama has crossed under
        if (famaMamaCrossUnder)
        {
            if (_tradeOpen && _isLong)
            {
                _logger.Information("fama crossed under mama, closing long position {Id}", _transactionId);
                var closePos = await _tradeManager.ClosePosition(AccountId);
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateClosePositionMessage(StrategyVersion,closePos, isLongPosition: true)));
                _tradeOpen = false;
            }
            
            if (!_tradeOpen)
            {
                var pos = await _tradeManager.OpenShortPosition(AccountId, "AUD_CHF" ,borrowAmount, StopLossInPips);


                if (string.IsNullOrEmpty(pos.ErrorCode))
                {
                    
                    _tradeOpen = true;
                    _transactionId = pos.OrderFillTransaction.TradeOpened.TradeID.ToString();
                    _logger.Information("fama crossed under mama, opening short position {Id}", _transactionId);
                    _isLong = false;
                }

                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateOpenOrderMessage(StrategyVersion,pos)));
            }

            return;
        }

        if (UseSecondaryTrigger)
        {
            await SecondaryTrigger(stockData, borrowAmount);    
        }
        
    }


    private async Task SecondaryTrigger(StockData stockData, double borrowAmount)
    {
        stockData.Clear();
        
        var rrsi = stockData.CalculateReverseEngineeringRelativeStrengthIndex(rsiLevel: RrsiLevel);
        
        
        var latest = rrsi.LatestValue("Rersi");
        // close crosses over
        if (stockData.ClosePrices[^2] < latest && stockData.ClosePrices[^1] > latest)
        {
            if (!_tradeOpen)
            {
                var pos = await _tradeManager.OpenLongPosition(AccountId, "AUD_CHF" ,borrowAmount, StopLossInPips);
                
                if (string.IsNullOrEmpty(pos.ErrorCode))
                {
                    _isLong = true;
                    _tradeOpen = true;
                    _transactionId = pos.OrderFillTransaction.TradeOpened.TradeID.ToString();
                    _logger.Information("close price crossed over rrsi,opening long position {Id}", _transactionId);
                }
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateOpenOrderMessage(StrategyVersion,pos)));
                return;
            }
        }

      
        // close crosses under 
        if (stockData.ClosePrices[^2] > latest && stockData.ClosePrices[^1] < latest)
        {
            if (_tradeOpen && _isLong)
            {
                _logger.Information("close price crossed under rrsi, closing long position {Id}", _transactionId);
                var closePos = await _tradeManager.ClosePosition(AccountId);
                OnSendMessage(new SystemMessageEventArgs(OrderMessageCreator.CreateClosePositionMessage(StrategyVersion,closePos, isLongPosition: true)));
                _tradeOpen = false;
            }
        }
        
    }

    public override async ValueTask DisposeAsync()
    {
        // add 5 pip take profit to the open trade
        if (_tradeOpen)
        {
            var trade = await _oandaApiConnection.TradeApi.GetTradeAsync(AccountId, _transactionId);
            // if position has profit, close
            if (trade.Trade.UnrealizedPL > 0)
            {
                await _tradeManager.ClosePosition(AccountId, _isLong);
                return;
            }
            
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

            await _oandaApiConnection.TradeApi.SetTradeOrdersAsync(AccountId, _transactionId,
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
