using System.ComponentModel.DataAnnotations;

namespace Tradeinator.Shared.Models;

/// <summary>
/// 
/// </summary>
/// <param name="DescriptiveName">Descriptive name</param>
/// <param name="PropertyName">The actual property name</param>
/// <param name="Value">the value</param>
/// <param name="Type">the simple type of property will be int, number, string or bool</param>
public record SerialisedProperty(
    string DescriptiveName, string PropertyName, object Value, string Type
);