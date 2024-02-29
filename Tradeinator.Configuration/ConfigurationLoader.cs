using Microsoft.Extensions.Configuration;

namespace Tradeinator.Configuration;


public class ConfigurationLoader
{
    private IConfiguration _configuration;
    private string _directory = AppContext.BaseDirectory;

    public IConfiguration Configuration => _configuration;
    
    /// <summary>
    /// Allows the use of an indexer same as on IConfiguration
    /// <example>
    ///  config["Rabbit:Host"}
    /// </example>
    /// <
    /// 
    /// </summary>
    /// <param name="i"></param>
    public string? this[string i] => Get(i);


    /// <summary>
    /// Loads the configuration
    /// </summary>
    /// <param name="rootDirectory">leave null to load from AppContext.BaseDirectory</param>
    /// <param name="envFile">name of .env file</param>
    /// <param name="appsettingsFile">name of appsettings.json file</param>
    public ConfigurationLoader(string? rootDirectory = null, string envFile = ".env", string appsettingsFile = "appsettings.json")
    {
        if (!string.IsNullOrEmpty(rootDirectory))
            _directory = rootDirectory;
        
        // load the dotenv file into the environment
        DotEnv.LoadEnvFiles(Path.Combine(_directory, envFile));

        // load the config
        _configuration = new ConfigurationBuilder()
            .SetBasePath(_directory)
            .AddJsonFile(Path.Combine(_directory, appsettingsFile), true)
            .AddEnvironmentVariables()
            .Build();        
    }
    
    /// <summary>
    /// gets the value from the config for the corresponding key
    /// </summary>
    /// <param name="key">the key to get</param>
    /// <returns>the value if found else null</returns>
    public string? Get(string key)
    {
        return _configuration[key];
    }
    
    
}