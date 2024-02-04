namespace Tradeinator.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SerialisableParameter: Attribute
{

    public object Value { get; set; }
    public string SerialsedName { get; set; }
    
    
    public SerialisableParameter(string name, object value)
    {
        SerialsedName = name;
        Value = value;
    }
}