using Tradeinator.Shared.Attributes;
using Tradeinator.Strategy.Shared;
using Xunit.Abstractions;

namespace Tradeinator.Strategy.Test;


public class SerialisationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SerialisationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    [Fact]
    public void GetAttributesAppliedToClass()
    {
        var strategy = new ExampleStrategy();
        var type = strategy.GetType();
        var props = type.GetProperties();

        foreach (var propertyInfo in props)
        {
            var attribute = (SerialisableParameter) Attribute.GetCustomAttribute(propertyInfo, typeof(SerialisableParameter));
            if (attribute is null)
            {
                _testOutputHelper.WriteLine($"Property {propertyInfo.Name} had no attributes applied");
            } 
            else
                _testOutputHelper.WriteLine($"{attribute.SerialsedName} | {attribute.Value}");
        }
    }
}