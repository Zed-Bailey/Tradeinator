namespace Tradeinator.Shared.Models;

public class SystemMessage
{
    public MessagePriority Priority { get; set; }
    public string Message { get; set; }
    public DateTime Time { get; set; }
}