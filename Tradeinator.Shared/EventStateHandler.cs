using System.ComponentModel.Design;
using Newtonsoft.Json;

namespace Tradeinator.Shared;

public class EventStateHandler
{
    // key is the topic
    // value is a tuple containing the object type and the callback function
    private Dictionary<string, (Type ObjType, Action<object> Callback)> _events = new();
    private Action<object>? _defaultCallback = null;


    /// <summary>
    /// Register a new callback for the specified type
    /// </summary>
    /// <param name="topic">the events topic or routing key</param>
    /// <param name="then">the callback, can be assumed that the object parameter is of type T</param>
    /// <typeparam name="T">the type to register</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">throws if this type has already been registered</exception>
    public EventStateHandler If<T>(string topic, Action<object> then) where T : class
    {
        if (_events.ContainsKey(topic))
            throw new ArgumentException($"An action has already been registered for the topic: {topic}");
        
        _events.Add(topic, (typeof(T), then));
        
        return this;
    }

    /// <summary>
    /// Register a callback that will be called in consume instead of throwing if no matching type is found
    /// </summary>
    /// <param name="defaultCallback">the action to invoke if an event that hasn't been registered is received</param>
    /// <returns></returns>
    public EventStateHandler Default(Action<object> defaultCallback)
    {
        _defaultCallback = defaultCallback;
        
        return this;
    }

    /// <summary>
    /// Consume an object and call the corresponding callback
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="json"></param>
    /// <exception cref="ArgumentException">will throw if default callback is not registered</exception>
    public void Consume(string topic, string json)
    {
        // check that the topic key exists
        if (!_events.ContainsKey(topic))
        {
            // check if default callback has been registered before throwing an exception
            if(_defaultCallback != null)
            {
                _defaultCallback.Invoke((topic, json));
                return;
            }
             
            throw new ArgumentException($"No action has been registered for the event: {topic}");

        }
        
        // get key value pair, deserialise object to stored type and invoke callback
        var kvp = _events[topic];
        var obj = JsonConvert.DeserializeObject(json, kvp.ObjType);
        kvp.Callback.Invoke(obj);
    }
}