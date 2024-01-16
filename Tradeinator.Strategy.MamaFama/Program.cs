// See https://aka.ms/new-console-template for more information

using Tradeinator.Strategy.Shared;

var configLoader = new ConfigurationLoader(Directory.GetCurrentDirectory());
configLoader.LoadConfiguration();

Console.WriteLine(configLoader.Get("OANDA_API_TOKEN"));