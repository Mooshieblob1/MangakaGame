using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;
public class JourneyMilestonesTests
{
    static GameEvent E(GameState s,EventType type,int? series=null,int? person=null)=>new(){Time=s.Clock.Now,ActivityDate=s.Clock.Now.Date,Type=type,SeriesId=series,PersonId=person};
    static int Own(GameState s){s.Apply(new CreateDoujinCommand("First pages","adventure"));return s.Series[^1].Id;}
    [Fact]public void Owned_series_events_map_to_milestones()
    {
        var s=GameState.NewGame(0);var own=Own(s);var tracker=new JourneyMilestones(s);
        var ids=tracker.Observe(s,[E(s,EventType.PitchSubmitted,own),E(s,EventType.PitchSubmitted,own),E(s,EventType.PitchRejected,own),
            E(s,EventType.SerializationOffered,own),E(s,EventType.OfferAccepted,own),E(s,EventType.ChapterPublished,own),E(s,EventType.ChapterPublished,own),
            E(s,EventType.DeadlineMissed,own),E(s,EventType.CancellationWarning,own),E(s,EventType.CancellationWarning,own),E(s,EventType.SeriesCancelled,own)]);
        Assert.Equal(["first-pitch","pitch-answer rejected","pitch-answer offered","serialization-accepted","first-magazine-chapter",
            "first-deadline-missed","cancellation-warning","cancellation-warning","cancellation"],ids);
    }
    [Fact]public void Other_studios_series_are_ignored()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);
        var rival=s.Series.FirstOrDefault(x=>x.BusinessId!=s.ControlledBusinessId)?.Id??-1;
        Assert.Empty(tracker.Observe(s,[E(s,EventType.PitchSubmitted,rival),E(s,EventType.CancellationWarning,rival),E(s,EventType.PitchSubmitted,null)]));
    }
    [Fact]public void Hire_and_payday_milestones()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);
        Assert.Empty(tracker.Observe(s,[E(s,EventType.StaffHired,person:s.ProtagonistPersonId)]));
        var hire=new Person{Id=s.People.Max(p=>p.Id)+1,Name="Test hire"};hire.EmploymentHistory.Add(new(){BusinessId=s.ControlledBusinessId,LocationId=s.Locations.First(l=>l.BusinessId==s.ControlledBusinessId).Id,StartsAt=s.Clock.Now,MonthlySalary=150_000});s.People.Add(hire);
        var ids=tracker.Observe(s,[E(s,EventType.StaffHired,person:hire.Id),E(s,EventType.StaffHired,person:hire.Id),E(s,EventType.WageArrears),E(s,EventType.WageArrears)]);
        Assert.Equal(["first-hire","missed-payday"],ids);
        s.Advance(24*32);
        Assert.Equal(["missed-payday"],tracker.Observe(s,[E(s,EventType.WageArrears)]));
    }
    [Fact]public void Poll_finds_completion_first_sale_and_later_sales()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);var ids=new List<string>();
        s.Apply(new CreateDoujinCommand("First pages","adventure"));var doujin=s.Series[^1];
        for(var d=0;d<180&&doujin.Volumes.Count==0;d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Contains("first-doujin-completed",ids);
        var v=doujin.Volumes.Single();
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:(int)PrintTier.CopyShop));
        for(var d=0;d<60&&v.CopiesSold==0;d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Equal(1,ids.Count(i=>i=="first-sale"));
        Assert.DoesNotContain("later-sale",ids);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:(int)PrintTier.CopyShop));
        var sold=v.CopiesSold;
        for(var d=0;d<90&&!ids.Contains("later-sale reprint");d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Contains("later-sale reprint",ids);
        Assert.True(v.CopiesSold>sold);
    }
    [Fact]public void JourneyMilestones_premarks_reached_milestones()
    {
        var s=GameState.NewGame(0);var own=Own(s);var first=new JourneyMilestones(s);
        var doujin=s.Series[^1];
        for(var d=0;d<180&&doujin.Volumes.Count==0;d++)s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print,doujin.Volumes[0].Id,Amount:10,Value:(int)PrintTier.CopyShop));
        for(var d=0;d<60&&doujin.Volumes[0].CopiesSold==0;d++)s.Advance(24);
        s.Events.Add(E(s,EventType.PitchSubmitted,own));
        // A freshly loaded career: nothing already reached is logged again.
        var loaded=GameState.FromJson(s.ToJson());var tracker=new JourneyMilestones(loaded);
        Assert.Empty(tracker.Observe(loaded,[]));
        Assert.Empty(tracker.Observe(loaded,[E(loaded,EventType.PitchSubmitted,own)]));
    }
}
