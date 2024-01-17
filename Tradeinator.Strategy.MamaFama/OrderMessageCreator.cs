using GeriRemenyi.Oanda.V20.Client.Model;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.MamaFama;

public class OrderMessageCreator
{

    public static SystemMessage CreateStopLossTriggeredMessage(string strategy, Trade trade)
    {
        var msg = new SystemMessage()
        {
            Priority = MessagePriority.Information,
            Symbol = "AUD/CHF",
            StrategyName = strategy,
        };

        msg.Message = $"""
                       Stop loss triggered for order = {trade.Id}
                       open time = {trade.OpenTime}
                       open price = {trade.Price}
                       
                       close time = {trade.CloseTime}
                       close price = {trade.AverageClosePrice}
                       
                       P/L = {trade.RealizedPL}
                       units = {trade.InitialUnits}
                       """;
        

        return msg;
    }
    
    public static SystemMessage CreateOpenOrderMessage(string strategy, CreateOrderResponse response)
    {
        
        var severity = response.ErrorCode == "201" ? MessagePriority.Information : MessagePriority.Critical;
        var msg = severity != MessagePriority.Information
            ? response.ErrorMessage
            : $"""
               Created a new order.
               order id = {response.OrderCreateTransaction.BatchID}
               fill price = $ {response.OrderFillTransaction.Price}
               units = {response.OrderFillTransaction.Units}
               financing = $ {response.OrderFillTransaction.Financing}
               """;
        return new SystemMessage()
        {
            Priority = severity,
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
                       Closed long position.
                       closed order = {response.LongOrderCreateTransaction.BatchID}
                       close price = {response.LongOrderFillTransaction.Price}
                       units = {response.LongOrderFillTransaction.Units}
                       P/L = $ {response.LongOrderFillTransaction.Pl}
                       Account Balance = $ {response.LongOrderFillTransaction.AccountBalance}
                       Commission = $ {response.LongOrderFillTransaction.Commission}
                       """
        };

        if(!isLongPosition)
        {
            msg.Message = $"""
                           Closed short position.
                           closed order = {response.ShortOrderCreateTransaction.BatchID}
                           close price = {response.ShortOrderFillTransaction.Price}
                           units = {response.ShortOrderFillTransaction.Units}
                           P/L = $ {response.ShortOrderFillTransaction.Pl}
                           Account Balance = $ {response.ShortOrderFillTransaction.AccountBalance}
                           Commission = $ {response.ShortOrderFillTransaction.Commission}
                           """;
        }


        return msg;


    }
}