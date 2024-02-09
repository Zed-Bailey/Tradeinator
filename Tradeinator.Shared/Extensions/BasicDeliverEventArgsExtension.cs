using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;


namespace Tradeinator.Shared.Extensions;

public static class BasicDeliverEventArgsExtension
{
    /// <summary>
    /// deserializes the message body into a model
    /// </summary>
    /// <param name="args">the event args</param>
    /// <typeparam name="T">the type to convert to</typeparam>
    /// <returns>the deserialized type, or null if deserialization failed</returns>
    public static T? DeserializeToModel<T>(this BasicDeliverEventArgs args) where T: class
    {
        try
        {
            // string should be a json string
            var body = args.BodyAsString();
            return JsonConvert.DeserializeObject<T>(body, new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Error
            });

        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    public static string BodyAsString(this BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        return Encoding.UTF8.GetString(body);
    }
    
}