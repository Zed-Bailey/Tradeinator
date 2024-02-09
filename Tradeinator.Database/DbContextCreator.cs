using Microsoft.EntityFrameworkCore;

namespace Tradeinator.Database;

public class DbContextCreator
{
    /// <summary>
    /// Helper method used by the strategies to simplify creating a new DbContext
    /// </summary>
    /// <param name="connectionString">database connection string</param>
    /// <returns></returns>
    public static ApplicationContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>();
        options.UseMySQL(connectionString);

        return new ApplicationContext(options.Options);
    }
    
}