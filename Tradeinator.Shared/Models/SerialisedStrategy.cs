using System.ComponentModel.DataAnnotations;

namespace Tradeinator.Shared.Models;

public class SerialisedStrategy
{
    [Key]
    public Guid SerialisedStrategyId { get; set; }
    
    public string StrategyToken { get; set; }
    public string StrategyName { get; set; }
    public string JsonConfig { get; set; }
}

/// <summary>
/// 
/// </summary>
/// <param name="Name">Descriptive name</param>
/// <param name="PropertyName">The actual property name</param>
/// <param name="Value">the value</param>
/// <param name="Type">the simple type of property will be int, number, string or bool</param>
public record SerialisedProperty(
    string Name, string PropertyName, object Value, string Type
);