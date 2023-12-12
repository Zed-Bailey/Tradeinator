using Alpaca.Markets;
using Serilog.Core;

namespace DataIngestion;

public static class StreamingClientExtensions
{
    public static void RegisterLoggers(this IAlpacaDataStreamingClient client, Logger logger)
    {
        client.Connected += status =>
        {
            if(status == AuthStatus.Authorized) 
                logger.Information("Connected and Authorised successfully");
            else if (status == AuthStatus.Unauthorized)
                logger.Error("Failed to connect. UnAuthorized");
            else
                logger.Error("Failed to connect due to to many connections");
        };

        client.SocketOpened += () => logger.Information("Socket opened");
        client.SocketClosed += () => logger.Information("Socket closed");
        client.OnError += exception => logger.Error(exception, "An exception occured, {SocketException}", exception);
        client.OnWarning += s => logger.Warning("{Warning}", s);


    }   
}