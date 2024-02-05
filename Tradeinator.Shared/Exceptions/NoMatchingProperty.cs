namespace Tradeinator.Shared.Exceptions;

public class NoMatchingProperty : Exception
{

    public NoMatchingProperty() : base() { }

    public NoMatchingProperty(string message) : base(message) { }
    
}