using Tradeinator.Shared;

public class MeanReversion: IStrategy
{
    public Task Init()
    {
        Console.WriteLine("mean reversion init function");
        
        return Task.CompletedTask;
    }
}