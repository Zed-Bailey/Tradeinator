using System.ComponentModel.DataAnnotations;

namespace Tradeinator.Database.Models;

public class SavedStrategy
{
    [Key]
    public int SavedStrategyId { get; set; }
    
    public string Slug { get; set; }
    public string StrategyName { get; set; }
    public string Config { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    
}