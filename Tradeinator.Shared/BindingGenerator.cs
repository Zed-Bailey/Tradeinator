namespace Tradeinator.Shared;

public static class BindingGenerator
{

    /// <summary>
    /// Converts a list of symbols to the bar topic binding format
    /// e.g. "AAPL" -> "bar.AAPL"
    /// </summary>
    /// <param name="symbols">symbols to convert</param>
    /// <returns>a string array containing the returned symbols</returns>
    public static string[] SymbolsToBarBindings(params string[] symbols)
    {
        return symbols
            .Select(x => $"bar.{x}")
            .ToArray();
    }
}