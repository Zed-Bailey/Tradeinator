using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Tradeinator.Configuration;

namespace Tradeinator.Database;


// required as the context and migrations are in a class library
public class ApplicationContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationContext> 
{ 
    public ApplicationContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationLoader();
         
        var builder = new DbContextOptionsBuilder<ApplicationContext>(); 
        var connectionString = config["ConnectionStrings:DbConnection"]; 
        builder.UseMySQL(connectionString); 
        return new ApplicationContext(builder.Options); 
    } 
}