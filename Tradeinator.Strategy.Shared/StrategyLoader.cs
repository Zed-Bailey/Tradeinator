namespace Tradeinator.Strategy.Shared;

public class StrategyLoader
{
    /*
     * connect to database
     * if no strategies added with strategy token, serialise strategy default editable parameter values and save to DB
     * else load strategies from DB
     *
     * 
     */

    public StrategyLoader()
    {
        
    }

    /// <summary>
    /// Loads the strategies from the database as type T and then configures properties with corresponding config values
    /// </summary>
    /// <param name="strategyToken">the token assigned to the strategy</param>
    /// <typeparam name="T">strategy type</typeparam>
    /// <returns></returns>
    public List<T> LoadStrategies<T>(string strategyToken)
    {
        
        return new List<T>();
    }
}