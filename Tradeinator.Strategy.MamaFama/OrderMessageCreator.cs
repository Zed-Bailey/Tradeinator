using GeriRemenyi.Oanda.V20.Client.Model;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.MamaFama;

public class OrderMessageCreator
{

    public static SystemMessage CreateStopLossTriggeredMessage(string strategy, Trade trade)
    {
        var msg = new SystemMessage
        {
            Priority = MessagePriority.Information,
            Symbol = "AUD/CHF",
            StrategyName = strategy,
            Message = $"""
                       Stop loss triggered for order = {trade.Id}
                       open time = {trade.OpenTime}
                       open price = {trade.Price}

                       close time = {trade.CloseTime}
                       close price = {trade.AverageClosePrice}

                       P/L = {trade.RealizedPL}
                       units = {trade.InitialUnits}
                       """
        };


        return msg;
    }
    
    public static SystemMessage CreateOpenOrderMessage(string strategy, CreateOrderResponse response)
    {
        
        
        var msg = $"""
                   Created a new order.
                   order id = {response.OrderCreateTransaction.BatchID}
                   time = {response.OrderCreateTransaction.Time}
                   """;
        return new SystemMessage()
        {
            Priority = MessagePriority.Information,
            Symbol = "AUD/CHF",
            StrategyName = strategy,
            Message = msg
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    /// <param name="isLongPosition">set to false for short position</param>
    /// <returns></returns>
    public static SystemMessage CreateClosePositionMessage(string strategy, ClosePositionResponse response, bool isLongPosition = true)
    {
        var msg = new SystemMessage()
        {
            Priority = MessagePriority.Information,
            Symbol = "AUD/CHF",
            StrategyName = strategy,
            Message = $"""
                       Closed {(isLongPosition ? "long" : "short")} position.
                       {response.ToJson()}
                       """
        };

        return msg;


    }
}