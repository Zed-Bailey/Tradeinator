using Microsoft.Extensions.Configuration;

namespace Tradeinator.Shared;

public class ConfigurationLoader
{
    private string _directory;
    private string _appsettings;
    private string _env;

    private IConfigurationRoot? _config;
    
    public ConfigurationLoader(string directory, string appsettings = "appsettings.json", string env = ".env")
    {
        _directory = directory;
        _appsettings = appsettings;
        _env = env;
    }


    public void LoadConfiguration()
    {
        // load the dotenv file into the environment
        DotEnv.LoadEnvFiles(Path.Combine(_directory, _env));

        // load the config
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine(_directory, _appsettings), true)
            .AddEnvironmentVariables()
            .Build();
    }

    public string? Get(string key)
    {
        if (_config is null) throw new NullReferenceException("Config hasn't been loaded yet");

        return _config[key];
    }
}