namespace Tradeinator.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SerialisableParameter: Attribute
{

    public object Value { get; set; }
    public string DescriptiveName { get; set; }
    
    
    public SerialisableParameter(string name, object value)
    {
        DescriptiveName = name;
        Value = value;
    }
}