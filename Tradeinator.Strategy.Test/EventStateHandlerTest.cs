using Tradeinator.Shared;
using Tradeinator.Shared.Models;
using Tradeinator.Shared.Models.Events;

namespace Tradeinator.Strategy.Test;

public class EventStateHandlerTest
{



    [Fact]
    public void RegisterDuplicateState_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var esh = new EventStateHandler()
                .If<Bar>("1", o => { })
                .If<Bar>("2", o => { });
        });
    }

    [Fact]
    public void ConsumeUpdateEvent_CallsCallback()
    {
        string updateJson = """
                      {"Slug":"MAMAFAMA","Id":1}
                      """;
        var updateRecord = new UpdateStrategyEvent("MAMAFAMA", 1);
        bool called = false;
        
        var esh = new EventStateHandler()
            .If<UpdateStrategyEvent>("update.MAMAFAMA",o =>
            {
                Assert.Equal(typeof(UpdateStrategyEvent), o.GetType());
                Assert.Equal(updateRecord, o);
                called = true;
            });
        
        esh.Consume("update.MAMAFAMA", updateJson);
        
        Assert.True(called);
    } 
    
}