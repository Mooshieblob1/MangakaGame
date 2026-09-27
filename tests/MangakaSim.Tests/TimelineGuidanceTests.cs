using Xunit;
namespace MangakaSim.Tests;
public class TimelineGuidanceTests
{
    static GuidanceMessage M(string step,int day)=>new(){Step=step,Time=new DateTime(1996,4,day)};
    [Fact]public void NewGuidance_lists_messages_after_the_last_seen_one()
    {
        var a=M("doujin",1);var b=M("print",2);var c=M("sell",3);
        Assert.Equal(["print","sell"],TimelineGuidance.NewSteps([a,b,c],a));
        Assert.Empty(TimelineGuidance.NewSteps([a,b,c],c));
        Assert.Equal(["doujin","print","sell"],TimelineGuidance.NewSteps([a,b,c],null));
    }
    [Fact]public void NewGuidance_handles_a_replaced_thread()
    {
        var old=M("doujin",1);var fresh=new[]{M("welcome",5),M("doujin",6)};
        // The last seen message is gone (a new career, load or trimmed thread): only the newest message is logged.
        Assert.Equal(["doujin"],TimelineGuidance.NewSteps(fresh,old));
        Assert.Empty(TimelineGuidance.NewSteps([],old));
    }
}
