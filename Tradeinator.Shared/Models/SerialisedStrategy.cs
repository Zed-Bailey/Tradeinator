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