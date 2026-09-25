using System.Text.Json.Nodes;
using Xunit;

namespace MangakaSim.Tests;

public class ProductionClarityTests
{
    private static GameState Ongoing(int pages=8)
    {
        var state=GameState.NewGame(2);
        state.Apply(new CreateSeriesCommand("Short chapters","drama",Cadence.Weekly,pages,true));
        return state;
    }
    [Fact] public void A_single_chapter_unlocks_an_issue_and_five_also_unlock_an_optional_collection()
    {
        var state=Ongoing();var series=state.Series[0];
        PublishingTests.Until(state,()=>series.Volumes.Count>0);
        var first=Assert.Single(series.Volumes);
        Assert.Equal(VolumeFormat.DoujinIssue,first.Format);Assert.Single(first.ChapterIds);
        Assert.Equal("Issue 1",GameState.EditionName(first));Assert.Equal(1,state.ChaptersTowardCollection(series));
        for(var hour=0;hour<24*100&&!series.Volumes.Any(v=>v.Format!=VolumeFormat.DoujinIssue);hour++)
        {
            foreach(var issue in series.Volumes.Where(v=>v.ReleasedAt is null&&!state.PrintRuns.Any(r=>r.VolumeId==v.Id)).ToArray())
                state.Apply(new StudioActionCommand(StudioAction.Print,issue.Id,Amount:1));
            state.Advance(1);
        }
        var collection=Assert.Single(series.Volumes,v=>v.Format!=VolumeFormat.DoujinIssue);
        Assert.Equal(5,collection.ChapterIds.Count);Assert.Equal("Collected book 1",GameState.EditionName(collection));
        Assert.Equal(5,series.Volumes.Count(v=>v.Format==VolumeFormat.DoujinIssue));
        foreach(var issue in series.Volumes.Where(v=>v.Format==VolumeFormat.DoujinIssue&&v.ReleasedAt is null&&!state.PrintRuns.Any(r=>r.VolumeId==v.Id)))
            state.Apply(new StudioActionCommand(StudioAction.Print,issue.Id,Amount:1));
        PublishingTests.Until(state,()=>series.Volumes.Count(v=>v.Format==VolumeFormat.DoujinIssue)>=6);
        Assert.Null(collection.ReleasedAt); // The optional collection never blocks the next issue.
        Assert.Equal(state.ToJson(),GameState.FromJson(state.ToJson()).ToJson());
        Assert.Equal(state.ToJson(),state.ReplayTimeline().ToJson());
    }
    [Fact] public void Personal_targets_do_not_cause_risk_overtime_or_late_flags()
    {
        var state=Ongoing(64);state.Advance(24*45);
        Assert.Contains(state.Series[0].Chapters,c=>c.CompletedAt>c.DueDate);
        Assert.All(state.Series[0].Chapters,c=>{Assert.False(c.IsAtRisk);Assert.False(c.IsLate);Assert.Equal(0,c.HoursOverdue);});
        Assert.DoesNotContain(state.Events,e=>e.Type is EventType.ChapterAtRisk or EventType.DeadlineMissed);
        Assert.All(state.Series[0].Chapters.SelectMany(c=>c.Stages),w=>Assert.Equal(0,w.OvertimeHours));
    }
    [Fact] public void Outside_shift_pays_personal_income_and_cannot_also_draw()
    {
        var state=Ongoing(16);state.Apply(new SetOutsideJobCommand(OutsideJob.Afternoons));state.Advance(4);
        var chapter=state.Series[0].Chapters[0];var progress=chapter.Stages.Sum(w=>w.HoursDone);
        var hours=state.Protagonist.ProductiveHours;var money=state.PersonalMoney;var business=state.Money;var rate=state.OutsideHourlyPay;
        state.Advance(4);
        Assert.Equal(money+rate*4,state.PersonalMoney);Assert.Equal(business,state.Money);
        Assert.Equal(progress,chapter.Stages.Sum(w=>w.HoursDone));Assert.Equal(hours,state.Protagonist.ProductiveHours);
        Assert.Contains(state.OfficeActivities,a=>a.PersonId==state.ProtagonistPersonId&&a.Kind==OfficeActivityKind.OutsideJob);
        state.Advance(1);Assert.True(state.Protagonist.ProductiveHours>hours);
        Assert.Equal(state.ToJson(),GameState.FromJson(state.ToJson()).ToJson());
        Assert.Equal(state.ToJson(),state.ReplayTimeline().ToJson());
    }
    [Fact] public void Shift_choice_is_optional_and_invalid_choices_are_atomic()
    {
        var state=Ongoing();state.Advance(7*24);
        Assert.DoesNotContain(state.Protagonist.PersonalAccount.Entries,e=>e.Reason=="part-time job");
        var before=state.ToJson();Assert.Throws<InvalidCommandException>(()=>state.Apply(new SetOutsideJobCommand((OutsideJob)99)));
        Assert.Equal(before,state.ToJson());
        state.Apply(new SetOutsideJobCommand(OutsideJob.Evenings));state.Advance(7*24);
        Assert.Equal(state.OutsideHourlyPay*12,state.Protagonist.PersonalAccount.Entries.Where(e=>e.Reason=="part-time job").Sum(e=>e.Amount));
        state.Apply(new SetOutsideJobCommand(OutsideJob.None));var count=state.Protagonist.PersonalAccount.Entries.Count(e=>e.Reason=="part-time job");state.Advance(7*24);
        Assert.Equal(count,state.Protagonist.PersonalAccount.Entries.Count(e=>e.Reason=="part-time job"));
    }
    [Fact] public void Studio_employment_suspends_outside_shifts()
    {
        var state=GameState.NewGame();state.Apply(new SetOutsideJobCommand(OutsideJob.Evenings));
        state.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));state.Advance(24);
        var previous=state.Protagonist.PersonalAccount.Entries.Count(e=>e.Reason=="part-time job");state.Advance(7*24);
        Assert.Equal(ControlMode.EmployedLead,state.Control);
        Assert.Equal(previous,state.Protagonist.PersonalAccount.Entries.Count(e=>e.Reason=="part-time job"));
    }
    [Fact] public void Convention_takes_only_selected_titles_stock_and_records_actual_sales()
    {
        var state=GameState.NewGame(2);
        foreach(var title in new[]{"A","B"})
        {
            state.Apply(new CreateDoujinCommand(title,"drama",8));var series=state.Series.Last();
            PublishingTests.Until(state,()=>series.Volumes.Count>0);
            state.Apply(new StudioActionCommand(StudioAction.Print,series.Volumes[0].Id,Amount:100));
        }
        state.Advance(24);var selected=state.Series[0];var other=state.Series[1];
        state.Apply(new StudioActionCommand(StudioAction.BookConvention,state.ProtagonistPersonId,Amount:selected.Id));
        var booking=Assert.Single(state.Bookings);Assert.Equal(0,booking.Fee+booking.TravelCost);
        state.Advance(state.Clock.HoursUntil(booking.Date.AddHours(-24)));
        state.Apply(new StudioActionCommand(StudioAction.Print,selected.Volumes[0].Id,Amount:100));
        state.Advance(state.Clock.HoursUntil(booking.Date.AddHours(11)));
        var otherStock=state.Stock(other.Volumes[0].Id);var copies=selected.Volumes[0].CopiesSold;var fans=selected.Fanbase;var hours=state.Protagonist.ProductiveHours;
        state.Advance(5+booking.TravelHours);
        Assert.True(booking.Settled);Assert.True(booking.CopiesSold>0,$"Staffed {booking.StaffedHours}; stock {state.Stock(selected.Volumes[0].Id)}; demand {selected.Volumes[0].WeeklyDemand}");
        Assert.Equal(copies+booking.CopiesSold,selected.Volumes[0].CopiesSold);
        Assert.Equal(otherStock,state.Stock(other.Volumes[0].Id));Assert.True(selected.Fanbase>fans);
        Assert.Equal(hours,state.Protagonist.ProductiveHours);
        Assert.Equal(state.ToJson(),GameState.FromJson(state.ToJson()).ToJson());
        Assert.Equal(state.ToJson(),state.ReplayTimeline().ToJson());
    }
    [Fact] public void Convention_can_promote_a_title_without_printed_stock()
    {
        var state=Ongoing();var series=state.Series[0];
        state.Apply(new StudioActionCommand(StudioAction.BookConvention,state.ProtagonistPersonId,Amount:series.Id));
        var booking=Assert.Single(state.Bookings);state.Advance(state.Clock.HoursUntil(booking.Date.AddHours(16+booking.TravelHours)));
        Assert.True(booking.Settled);Assert.Equal(0,booking.CopiesSold);Assert.Equal(5,series.Fanbase);
    }
    [Fact] public void Version8_careers_gain_the_balance_changes_without_losing_progress_or_records()
    {
        var state=GameState.NewGame(2);state.Apply(new CreateSeriesCommand("Existing","drama",Cadence.Weekly,16));state.Advance(4);
        foreach(var stage in StageOrder.All)state.Protagonist.Skills[stage]=80;
        state.Protagonist.Skills[Stage.Inks]=99;
        var chapter=state.Series[0].Chapters[0];chapter.IsAtRisk=true;
        var json=JsonNode.Parse(state.ToJson())!;json["Version"]=8;
        var imported=GameState.ImportSupported(json.ToJsonString());
        Assert.Equal(95,imported.Protagonist.Skill(Stage.Name));Assert.Equal(99,imported.Protagonist.Skill(Stage.Inks));
        Assert.True(imported.Series[0].ReleaseShortIssues);Assert.False(imported.Series[0].Chapters[0].IsAtRisk);
        Assert.Equal(chapter.Stages[0].HoursDone,imported.Series[0].Chapters[0].Stages[0].HoursDone);
        Assert.Equal(state.Money,imported.Money);Assert.Equal(state.PersonalMoney,imported.PersonalMoney);
        Assert.Equal(state.Events.Count,imported.Events.Count);Assert.Equal(state.CommandLog.Count,imported.CommandLog.Count);
        imported.Apply(new SetOutsideJobCommand(OutsideJob.Afternoons));imported.Advance(24);
        Assert.Equal(imported.ToJson(),GameState.FromJson(imported.ToJson()).ToJson());
        Assert.Equal(imported.ToJson(),imported.ReplayTimeline().ToJson());
    }
}
