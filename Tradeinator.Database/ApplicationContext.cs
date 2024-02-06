using Microsoft.EntityFrameworkCore;
using Tradeinator.Database.Models;

namespace Tradeinator.Database;

public class ApplicationContext : DbContext
{
    
    public DbSet<SavedStrategy> SavedStrategies { get; set; }
    
    
    

    public ApplicationContext(DbContextOptions<ApplicationContext> options): base(options)
    { }

    
}