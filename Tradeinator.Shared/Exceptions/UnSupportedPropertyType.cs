namespace Tradeinator.Shared.Exceptions;

public class UnSupportedPropertyType : Exception
{
    public UnSupportedPropertyType() : base() { }

    public UnSupportedPropertyType(string message) : base(message) { }
}