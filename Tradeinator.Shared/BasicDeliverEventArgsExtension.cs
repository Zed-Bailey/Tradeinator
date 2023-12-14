using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace Tradeinator.Shared;

public static class BasicDeliverEventArgsExtension
{
    /// <summary>
    /// deserializes the message body into a model
    /// </summary>
    /// <param name="args">the event args</param>
    /// <typeparam name="T">the type to convert to</typeparam>
    /// <returns>the deserialized type, or null if deserialization failed</returns>
    public static T? SerializeToModel<T>(this BasicDeliverEventArgs args)
    {
        try
        {
            // string should be a json string
            var body = args.BodyAsString();
            
            return JsonSerializer.Deserialize<T>(body);
        }
        catch (Exception e)
        {
            return default;
        }
    }

    public static string BodyAsString(this BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        return Encoding.UTF8.GetString(body);
    }
    
}