namespace Tradeinator.Dashboard.Data;

public record ExchangeEvent(
    string Topic,
    string Body,
    DateTime Time,
    string Key
);