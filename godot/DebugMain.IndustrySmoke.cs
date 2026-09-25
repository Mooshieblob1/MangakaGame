using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;
using MangakaSim.Catalog;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    private static GameState IndustryDateFixture(DateTime at)
    {
        var s=GameState.NewGame(19);s.Clock.Now=at;s.Candidates.Clear();s.Version=4;
        foreach(var m in s.Markets)
        {
            var d=s.PublisherCatalog.Get(m.MagazineId);m.NextIssueClose=IssueSchedule.FirstCloseAfter(d,at);
            m.IssuesClosed=(int)((m.NextIssueClose-IssueSchedule.Anchor(d)).TotalDays/IssueSchedule.Days(d.Cadence));
            m.LastRanking=m.Fillers.Select((f,i)=>new RankEntry(i+1,f.Title,null,f.Id,f.Popularity)).ToList();
        }
        return GameState.ImportTimelineV4(s.ToJson());
    }
    private async Task RunIndustrySmoke()
    {
        _state=GameState.NewGame(19);_scanIndex=_state.Events.Count;_log.Clear();_recapDialog.Hide();Pause();_mainTabs.CurrentTab=6;_dirty=true;
        await SettleUi();var before=_state.ToJson();RefreshIndustry();Check(before==_state.ToJson(),"Industry snapshots do not mutate or reroll");
        Check(!_industryRivals.Text.Contains("Two Piece")&&!_assistantProfile.Text.Contains("Oga"),"Future rivals and undiscovered assistants stay hidden");
        Check(_rivalProfile.Text.Contains("Scout for"),"Rival skills start undiscovered");
        Press("Scout · ¥10,000 / 3 days");Check(_state.Money==290000,"Scouting charges business exactly once");
        AdvanceAndScan(72);await SettleUi();Check(_rivalProfile.Text.Contains("Willingness unknown"),"Scouting report arrives in the UI");
        var target=_state.FindPerson(_rivalChoice.GetSelectedId())!;_industrySalary.Value=target.ExpectedSalary;
        Press("Approach · ¥20,000 / 7 days");Check(_state.World.Offers.Count==1&&_speed==0,"Approach creates a visible decision and pauses");
        _state.World.Offers[0].Roll=0;
        _state.Advance(24*7);_dirty=true;await SettleUi();Check(target.Employment!.BusinessId==_state.ControlledBusinessId,"Successful approach transfers the existing person");
        _state.ControlledBusiness.Account.OpeningBalance+=2000000;_state.Money+=2000000;
        _state.Apply(new StudioActionCommand(StudioAction.Move,1));var location=_state.Protagonist.Employment!.LocationId;
        var arrange=_state.ArrangeOffice(location,true,true);_state.Apply(new ApplyOfficeLayoutCommand(location,_state.OfficeRevision,arrange.Placements,arrange.Purchases,[]));
        _state.World.Assistants[0].DiscoveredBy=_state.ControlledBusinessId;_dirty=true;await SettleUi();
        var definition=TimelineCatalog.Default.Creators[0];_industrySalary.Value=StudioRules.ExpectedSalary(definition.Skills.Max());
        Check(_assistantProfile.Text.Contains("fixed departure")&&_assistantProfile.Text.Contains("fictional"),"Temporary contract terms are disclosed before hiring");
        Press("Hire temporary assistant");Check(_state.World.Assistants[0].PersonId is not null,"Temporary assistant hire control");
        var rivalLocation=_state.Locations.First(l=>_state.World.RivalBusinesses.Contains(l.BusinessId));
        var offer=new StaffNegotiation {Id=_state.NextId++,PersonId=target.Id,FromBusiness=_state.ControlledBusinessId,ToBusiness=rivalLocation.BusinessId,
            LocationId=rivalLocation.Id,CreatedAt=_state.Clock.Now,EndsAt=_state.Clock.Now.AddDays(7),Salary=target.ExpectedSalary,Roll=.999};
        _state.World.Offers.Add(offer);_dirty=true;await SettleUi();_industrySalary.Value=target.ExpectedSalary*1.2;
        Press("Offer selected salary to retain");Check(target.Employment!.MonthlySalary==(long)_industrySalary.Value,"Retention control updates monthly salary");
        var industryScroll=(ScrollContainer)_mainTabs.GetChild(6);industryScroll.ScrollVertical=600;
        await CaptureSmokeImage("industry-recruitment");industryScroll.ScrollVertical=0;
        // Isolate an urgent warning in the night skip, with ordinary news delivered in the same hour.
        _state=GameState.NewGame(19);_state.Advance(24*7+12);var destination=_state.Locations.First(l=>_state.World.RivalBusinesses.Contains(l.BusinessId));
        var pending=new StaffNegotiation {Id=_state.NextId++,PersonId=_state.ProtagonistPersonId,FromBusiness=_state.ControlledBusinessId,ToBusiness=destination.BusinessId,
            LocationId=destination.Id,CreatedAt=_state.Clock.Now.AddDays(-6).AddHours(1),EndsAt=_state.Clock.Now.AddDays(1).AddHours(1),Salary=200000};
        _state.World.Offers.Add(pending);_scanIndex=_state.Events.Count;SetSpeed(8);
        AdvanceAndScan(12);Check(_state.Clock.Hour==21&&_speed==0&&pending.Warned,"Idle advance stops at the exact urgent warning hour at 8x");
        SetSpeed(8);AdvanceAndScan(1);Check(_speed==8,"Acknowledged warning does not pause twice");Pause();
        _state=IndustryDateFixture(new(2006,5,1,8,0,0));_scanIndex=_state.Events.Count;_state.Apply(new GetOnlineCommand());
        _state.Apply(new CreateSeriesCommand("Mobile pages","drama",Cadence.Weekly,1));
        while(_state.Series[0].Volumes.Count==0)_state.Advance(24);
        _state.Series[0].Fanbase=10000;_dirty=true;await SettleUi();Press("Propose digital · ¥30,000");
        Check(_state.World.Channels.Single().Status==NegotiationStatus.Accepted,"Owner digital release activates from proposal control");
        _state.Advance(24*7);_dirty=true;await SettleUi();Check(_industryChannels.Text.Contains("channel units")&&_state.World.Receipts.Sum(r=>r.Units)>0,"Distinct digital receipts appear in channel report");
        Save();var saved=_state.ToJson();_state.Advance(1);Load();await SettleUi();Check(saved==_state.ToJson(),"Channel state round trips through Save/Load");
        industryScroll.ScrollVertical=(int)industryScroll.GetVScrollBar().MaxValue;await CaptureSmokeImage("industry-digital");industryScroll.ScrollVertical=0;
        _state=IndustryDateFixture(new(2025,12,31,23,0,0));_scanIndex=_state.Events.Count;SetSpeed(1);AdvanceAndScan(1);_dirty=true;await SettleUi();
        Check(_speed==1&&_industryNews.Text.Contains("SIMULATED FUTURE"),"Future cutoff is labeled and ordinary news does not pause");Pause();
        await CaptureSmokeImage("industry-future");
    }
}
