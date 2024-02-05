using System.Text.Json;
using Tradeinator.Shared.Attributes;
using Tradeinator.Shared.Exceptions;
using Tradeinator.Shared.Models;

namespace Tradeinator.Strategy.Shared;

public class StrategyLoader
{


    /// <summary>
    /// Will update an existing strategy object with the values in teh config object
    /// </summary>
    /// <param name="strategy">The existing strategy to update</param>
    /// <param name="newJsonConfig">The new json config</param>
    /// <typeparam name="T">the strategy type</typeparam>
    /// <exception cref="ArgumentException">throws when failed to deserialise the json</exception>
    /// <exception cref="NoMatchingProperty">throws when no property matches in the class</exception>
    public void UpdateStrategyProperties<T>(T strategy, string newJsonConfig)
    {
        var properties = JsonSerializer.Deserialize<SerialisedProperty[]>(newJsonConfig);
        if (properties == null)
            throw new ArgumentException($"Failed to deserialise json config.\n {newJsonConfig}", nameof(newJsonConfig));
        
        foreach (var property in properties)
        {
            var p = strategy.GetType().GetProperty(property.PropertyName);
            if (p == null)
                throw new NoMatchingProperty(
                    $"No property was found on {nameof(strategy)} with name {property.PropertyName}");
            // as the property is an object type, when deserialised it is converted to a JsonElement by System.Text.Json
            var element = (JsonElement)property.Value;
            
            // deserialise the element to the property type we have saved
            // todo: handle type being null
            
            var actualValue = element.Deserialize(Type.GetType(property.Type));
            
            // set the value of the property on the object
            p.SetValue(strategy,  actualValue);
        }

        return;
    }
    
    
    /// <summary>
    /// Initialises a strategy of type T and populates it's properties based on the json config
    /// </summary>
    /// <param name="json">The json config</param>
    /// <typeparam name="T">The type</typeparam>
    /// <exception cref="NoMatchingProperty">thrown when no property was found matching the serialised properties PropertyName value</exception>
    /// <returns>The deserialised strategy</returns>
    public T LoadStrategy<T>(string json) where T : new()
    {
        var properties = JsonSerializer.Deserialize<SerialisedProperty[]>(json);
        if (properties == null)
            throw new ArgumentException($"Failed to deserialise json config.\n {json}", nameof(json));
        
        // where T : new() indicates that the class must have a parameterless constructor
        var obj = new T();
        
        foreach (var property in properties)
        {
            var p = obj.GetType().GetProperty(property.PropertyName);
            if (p == null)
                throw new NoMatchingProperty(
                    $"No property was found on {nameof(obj)} with name {property.PropertyName}");
            // as the property is an object type, when deserialised it is converted to a JsonElement by System.Text.Json
            var element = (JsonElement)property.Value;
            
            // deserialise the element to the property type we have saved
            // todo: handle type being null
            var actualValue = element.Deserialize(Type.GetType(property.Type));
            
            // set the value of the property on the object
            p.SetValue(obj,  actualValue);
        }
        

        return obj;
    }


    /// <summary>
    /// Serialises a strategy into a json config string that can be saved to a database
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns></returns>
    public string? SerialiseStrategy(StrategyBase strategy)
    {
        var type = strategy.GetType();
        var props = type.GetProperties();
        var serialisedProperties = new List<SerialisedProperty>();
        foreach (var propertyInfo in props)
        {
            // get attribute applied to property
            var attribute = (SerialisableParameter?) Attribute.GetCustomAttribute(propertyInfo, typeof(SerialisableParameter));
            
            // no attribute applied to this property
            if (attribute is null) continue;
            
            serialisedProperties.Add(new SerialisedProperty(
                    attribute.DescriptiveName, propertyInfo.Name , propertyInfo.GetValue(strategy), propertyInfo.PropertyType.ToString()
                )
            );
            
        }

        
        return JsonSerializer.Serialize(serialisedProperties); 
    }
}