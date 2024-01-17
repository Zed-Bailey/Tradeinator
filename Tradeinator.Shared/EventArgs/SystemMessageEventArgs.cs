using Tradeinator.Shared.Models;

namespace Tradeinator.Shared.EventArgs;

public class SystemMessageEventArgs : System.EventArgs
{
    public SystemMessage Message { get; set; }

    public SystemMessageEventArgs(SystemMessage msg)
    {
        Message = msg;
    }
}