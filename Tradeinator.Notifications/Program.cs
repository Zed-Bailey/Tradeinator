using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tradeinator.Shared;
using Tradeinator.Shared.Models;

// load the dotenv file into the environment
DotEnv.LoadEnvFiles(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

// load the config
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true)
    .AddEnvironmentVariables()
    .Build();

// create serilog logger with file and console output
await using var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("notification.log")
    .CreateLogger();


var tokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.Information("Cancellation requested");
    tokenSource.Cancel();
};




var discordToken = config["DISCORD_TOKEN"] ?? throw new ArgumentNullException("Discord Token (key: DISCORD_TOKEN) was null");
// id of the channel where messages are sent
var channelIdStr = config["DISCORD_CHANNEL_ID"] ?? throw new ArgumentNullException("Discord channel id (key: DISCORD_CHANNEL_ID) was null");
ulong channelId;
if (!ulong.TryParse(channelIdStr, out channelId))
{
    throw new Exception($"Failed to convert channel id '{channelIdStr}' to a ulong");
}

var discord = new DiscordClient(new DiscordConfiguration()
{
    Token = discordToken,
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
});

// the channel where messages will be received
DiscordChannel? receivingChannel = null;





var host = config["Rabbit:Host"] ?? throw new ArgumentNullException("Rabbit host was null");
var exchangeName = config["Rabbit:Exchange"] ?? throw new ArgumentNullException("Rabbit exchange name was null");

using var exchange = new ReceiverExchange(host, exchangeName, "notification.*");
exchange.ConsumerOnReceive += (sender, eventArgs) =>
{
    var msg = eventArgs.DeserializeToModel<SystemMessage>();
    if (msg == null)
    {
        logger.Warning("System Message was null, tried to deserialise string: {OriginalMessage}", eventArgs.BodyAsString());
        return;
    }
    
    // split the routing key on the '.' to get the symbol the message is for
    var routingKey = eventArgs.RoutingKey.Split('.');
    var symbol = routingKey.Length > 1 ? routingKey[1] : eventArgs.RoutingKey;

    // build the message body
    var msgBuilder = new DiscordMessageBuilder()
        .WithContent($"""
                     **{symbol}**
                     *Priority*: {msg.Priority}
                     *Time*: {msg.Time}
                     {msg.Message}
                     """);
    
    // add @ on critical message priority
    if (msg.Priority == MessagePriority.Critical)
    {
        msgBuilder = msgBuilder.WithAllowedMention(EveryoneMention.All);
        msgBuilder.Content += $"\n\n@everyone";
    }
    
    msgBuilder.SendAsync(receivingChannel).Wait();
};

// ping pong bot message response, used to validate that the bot is connected and can send and receive messages
discord.MessageCreated += async (sender, eventArgs) =>
{
    if (eventArgs.Message.Content.ToLower().StartsWith("ping"))
        await eventArgs.Message.RespondAsync("pong");
};


// log the bot in
await discord.ConnectAsync();

try
{
    receivingChannel = await discord.GetChannelAsync(channelId);
}
catch (Exception e)
{
    logger.Error(e, "Failed to get the channel");
    return;
}


// start consuming events
await exchange.StartConsuming(tokenSource.Token);
await discord.DisconnectAsync();