namespace Tradeinator.Shared.Extensions;

public static class DoubleTypeExtension
{
    public static decimal ToDecimal(this double d)
    {
        return (decimal) d;
    }
}