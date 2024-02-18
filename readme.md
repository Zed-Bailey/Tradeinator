# Tradeinator

This is a project i have been wanting to work on for a while.

The aim of the project is to provide a modular approach to developing algorithmic trading systems.

The project utilises an event driven architecture and uses RabbitMQ as the message bus.

The use of an event driven architecture provides a modular approach to the system allowing me to 
add/remove/edit modules and strategies without having to stop and restart other systems

For stock indicators the following library is used: https://github.com/ooples/OoplesFinance.StockIndicators

## Strategy Research
Most of the strategy research is done on TradingView using Pinescript and then transferred to C#.



## Topics
Event topics should follow the general format of `{action}.{symbol}`

eg. `bar.AAPL` is the topic for a new Apple bar

## Adding new symbols
To add new symbols without having to stop and restart the system a file watcher is implemented
using the [.net file system watcher](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-8.0)
the `symbols.txt` file in the stocks/crypto `DataIngestion` project is watched.
The system has a file watcher registered to this file, when the file changes symbol subscriptions are added or removed

## Improvements
- strategy init message
- change console programs to worker services?
- add systemd service files to each project that can be easily copied
- solution level config file or a project that contains the config files and a class to load them [DONE]

## Configuration
All config files are located in the `Tradeinator.Configuration` project.

This provides a central place for all config files and reduces the need to duplicate them across projects

The configuration is loaded as an IConfiguration from Microsoft.Extensions.Configuration and loads everything from the `appsettings.json` file and `.env` file

Configuration files are copied to the build directory.


An example of using the config loader
```csharp
using Tradeinator.Configuration;

// config is loaded
var config = new ConfigurationLoader();

// config can be accessed using indexing
config["Rabbit:Host"]; // will return the host from the appsettings.json file

// or with the .Get(string key) method
config.Get("Rabbit:Host"); // will return the same value as the index call

```


## Tradeinator.Listener
Provides a simple example of registering to an topic and consuming data

## Tradeinator.Backtester
Provides a framework for quickly backtesting strategies using a forked version of the `SimpleBackTestLib` library

Strategies inherit from the BacktestRunner abstract class and must have the BacktestStrategyMetadata attribute added for it to be discoverable.
The meta data attribute defines some properties of the strategy such as it's name and budget.

When running the backtest the assembly is scanned for all classes that have the attribute applied. 
Options are then displayed allowing you to select which backtest to run (see image below)

<img src="docs/images/backtest_runner_example.png" title="an example of the options displayed to the user"/>

`Spectre.Console` is used to pretty print options to the console

### Improvements
- update to use a sqlite database to load data from rather than reading from csv files
- provide utility functionality to load data from csv into database
- provide options to select a data source rather then passing as cli argument

## Tradeinator.Dashboard
A blazor server web application built with the Microsofts Fluent UI.

Will provides a dashboard showing account balances, trades and more

## Tradeinator.EventTester
A Terminal GUI program to connect to the exchange and easily send test events
<img alt="event tester demo image" src="docs/images/event_tester_demo.png"/>
Built with the `Terminal.Gui` library

## Tradeniator.Database
This class library contains the EntityFramework DbContext and related migrations for a MySQL database.

The project also contains a design time factory so the EF migration command can be run from the project directly

Database connection is defined in the `Tradeinator.Configuration` project
```bash
cd Tradeinator.Database/
dotnet ef database update
```


## Tradeinator.DataIngestion.Shared
Contains the implementation of the file watching and subscription management

## Tradeinator.DataIngestion.Crypto
This is the entry point for all crypto data into the system.

Data is fetched from alpaca through a web socket conenction. Currently websockets subscribe to a 1m bar granularity, this will be updated in future.

Symbols to subscribe to are registered in the projects `symbols.txt` file

## Tradeinator.DataIngestion.Stocks
This is the entry point for all stock data into the system.

Data is fetched from alpaca through a web socket conenction. Currently websockets subscribe to a 1m bar granularity, this will be updated in future.

Symbols to subscribe to are registered in the projects `symbols.txt` file

## Tradeinator.DataIngestion.Forex
This is the entry point for all forex data into the system.

Data is fetched from Oanda, currently only 30 minute bars are supported.

Symbols to subscribe to are registered in the projects `symbols.txt` file


## Tradeinator.Shared
This project provides shared classes for the solution, such as the RabbitMQ exchange implementations and some generic models


## Tradeniator.Strategy.Shared
This class library contains the meat of the strategies.
It provides a base class called `StrategyBase` that all strategies must inherit from.
Strategies are run by creating a new `StrategyBuilder` and adding the the required properties.
When the `Build` method is called, the builder will fetch the strategies from the database and deserialise them into the provided strategy type


### Creating a strategy
create a new project and add a reference to the `Tradeniator.Strategy.Shared` and `Tradeniator.Shared` projects
then in your `Program.cs` file copy the following example.
As reference the `StrategyImplementingType` type is the name of the class that inherits from `StrategyBase` and implements the required methods.
This should be changed to the name of your class.

```csharp

// A readable ID that denotes this group of strategies
// used for filtering your strategies in the dashboard and registering events
const string StrategySlug = "StrategyGroupName";

// load config from appsettings.json and .env files
var configLoader = new ConfigurationLoader();

var apiToken = configLoader.Get("OANDA_API_TOKEN");
var exchangeHost = configLoader.Get("Rabbit:Host");
var exchangeName = configLoader.Get("Rabbit:Exchange");
var connectionString = configLoader.Get("ConnectionStrings:DbConnection");

if (ValidateNotNull(apiToken))
{
    Console.WriteLine("[ERROR] Oanda api token was null or empty");
    return;
}

if (ValidateNotNull(exchangeHost, exchangeName))
{
    Console.WriteLine("[ERROR] exchange host or name was null");
    return;
}

if (ValidateNotNull(connectionString))
{
    Console.WriteLine("[ERROR] db connection string was null");
    return;
}

// serilog logger
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("general.log")
    .CreateLogger();


var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancel requested");
    tokenSource.Cancel();
};


// setup exchange
using var exchange = new PublisherReceiverExchange(
    exchangeHost, exchangeName,
    "bar.AUD/CHF"
);


var strategies = new StrategyBuilder<StrategyImplementingType>(connectionString, logger)
    .WithSlug(StrategySlug) // the slug of the strategy
    .WithExchange(exchange) // will register a listener to consume change events
    .WithMessageNotificationCallback(OnSendMessageNotification) // register a callback for the send message notification event
    .WithDefaultStrategy(new StrategyImplementingType()) // register default strategy
    .Build();

// calls the Init method on all the strategies
// the init method will provide a configuration to the strategy so environment and appsettings variables can be loaded
await strategies.Init(); 

logger.Information("Initialising strategies");
logger.Information("initialised");


logger.Information("Exchange starting");
await exchange.StartConsuming(tokenSource.Token);
logger.Information("Exchange stopped");


return;


void OnSendMessageNotification(object? sender, SystemMessageEventArgs e)
{
    var message = e.Message;
    exchange.Publish(message, $"notification.{message.Symbol}");
}

bool ValidateNotNull(params string?[] values) => values.Any(string.IsNullOrEmpty);

```

## Tradeinator.Notifications

This project implements a notification system using a discord bot,
it listens to all events with the `notification.*` topic and publishes the events to discord.

A notification is the `Tradeinator.Shared.SystemMessage` class.
When the SystemMessages priority is `Critical` then the notification will add an `@everyone` mention

---

## Tradeinator.Logger
**This project is not currently implemented**

This project will subscribe to all order events (topic key: `order.*`) and log them to a database

