namespace Tradeinator.Dashboard.Data;

public class NewExchangeEventArgs : EventArgs
{
    public ExchangeEvent NewEvent { get; set; }

    public NewExchangeEventArgs(ExchangeEvent e)
    {
        NewEvent = e;
    }
}