namespace Tradeinator.Shared.Models.Events;

public record UpdateStrategyEvent(
    string Slug,
    int Id
);