namespace Tradeinator.Shared.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SerialisableParameter: Attribute
{

    public object Value { get; set; }
    public string DescriptiveName { get; set; }
    
    
    public SerialisableParameter(string descriptiveName = "")
    {
        DescriptiveName = descriptiveName;
    }
}