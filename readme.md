# Tradeinator

This is a project i have been wanting to work on for a while.

The aim of the project is to provide a modular approach to developing algorithmic trading systems.
The project makes use of RabbitMQ to provide a pub/sub message bus that should allow for the easy implementation
of multiple trading solutions from a single data source

For stock indicators the following library is used: https://github.com/ooples/OoplesFinance.StockIndicators


## Adding new symbols
To add new symbols without having to stop and restart the system the implementation of a filewatcher is crucial.
using the [.net file system watcher](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-8.0)
the appsettings.json file or another file can be watched. Subscribing to the onchanged event, the system can parse the symbols and dynamically 
add/remove data subscriptions without having to restart




## Tradeinator.DataIngestion
This is the entry point for all data into the system. 
This project will subscribe to data streams and publish messages to the rabbitmq exchange.

Exchange topics will generally follow the following format `{action}.{symbol}`, eg. `bar.aapl` this is the topic fr receiving a new apple bar


## Tradeinator.Shared
This project provides shared classes for the solution, such as the exchange implementations 


## Tradeinator.Notifications
**This project is not currently implemented**

The aim of this project will be to implement a discord notification system, this will provide live logs for buy and sell orders
or when a critical error occurs

## Tradeinator.Logger
**This project is not currently implemented**

This project will subscribe to all order events (topic key: `order.*`) and log them to a database

