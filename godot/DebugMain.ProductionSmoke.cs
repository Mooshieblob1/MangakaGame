using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunProductionSmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"production-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            foreach(var name in new[]{"Staff","Studios","Industry"})
            {
                Press(name);await SettleUi();
                Check(!_report.Visible&&_expanded&&_homeOffice.Visible&&_side.Visible,name+" opens its full overview with one click");
            }
            Press("Finances");await SettleUi();
            Check(!_report.Visible&&_expanded&&_homeOffice.Visible,"Finances fills the management area");
            ButtonNamed("▸  Part-time work & personal contributions").ButtonPressed=true;
            var schedule=_sideContent.FindChildren("*","OptionButton",true,false).OfType<OptionButton>().Single(o=>o.GetItemText(0).StartsWith("No outside job"));
            schedule.Select(1);Press("Apply part-time schedule");
            Check(_state.Protagonist.OutsideJob==OutsideJob.Afternoons,"Part-time choice applies through its control");
            await CaptureSmokeImage("production-finances");
            _navigation["Series"].EmitSignal(BaseButton.SignalName.Pressed);await SettleUi();
            Check(!_report.Visible&&_expanded&&_homeOffice.Visible,"Series rail entry opens the full overview");
            Press("+ Ongoing series");await SettleUi();
            _sideContent.FindChildren("OngoingTitle","LineEdit",true,false).OfType<LineEdit>().Single().Text="Tokyo notebook";
            Press("Create ongoing series");await SettleUi();var series=_state.Series.Single();
            Check(series.ReleaseShortIssues&&!series.StandaloneDoujin&&series.PagesPerChapter==16,"Ongoing form creates short chapter issues");
            _state.Advance(4);_dirty=true;await SettleUi();
            Check(_progressDescription.Text.Contains("Away at part-time job"),"Live progress explains absence");
            var bars=_sideContent.FindChildren("*","ProgressBar",true,false).OfType<ProgressBar>().ToArray();
            Check(bars.Length>0&&bars.All(b=>b is ChapterProgressBar||b.ShowPercentage)&&bars.Any(b=>b.Value>0),"Series sidebar progress has numbers inside the bars");
            await CaptureSmokeImage("production-series-progress");
            var start=_state.Clock.Now;
            for(var h=0;h<24*60&&series.Volumes.Count==0;h++)_state.Advance(1);
            Check(series.Volumes.Count==1&&series.Volumes[0].Format==VolumeFormat.DoujinIssue,"One actual chapter unlocks Issue 1");
            Check(!_state.Events.Any(e=>e.Type==EventType.DeadlineMissed),"Uncontracted opening has no missed-deadline penalty");
            _scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;_popupEvents.Clear();_dirty=true;await SettleUi();
            Press("Books");await SettleUi();Press("Print physical copies");await SettleUi();
            Check(_expanded&&_homeOffice.Visible,"Printing opens full width");
            _alphaCopies.Value=10;Press("Order this print run");_state.Advance(24);_scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;_dirty=true;await SettleUi();
            Check(_state.Stock(series.Volumes[0].Id)==10,"Printed chapter stock arrives");
            await CaptureSmokeImage("production-issue-printing");
            Press("Send to convention");await SettleUi();
            Check(_page=="Conventions"&&_sideContent.FindChildren("*","OptionButton",true,false).OfType<OptionButton>().First().GetSelectedId()==series.Id,"Direct booking carries selected title");
            Check(!ButtonNamed("Confirm convention booking").Disabled,"Nearby event can be booked");
            var quote=string.Join("\n",_sideContent.FindChildren("*","Label",true,false).OfType<Label>().Select(l=>l.Text));
            Check(quote.Contains("Booth ¥0 + travel ¥0 = ¥0")&&quote.Contains("10 copies"),"Booking shows total cost and available copies together");
            GetWindow().Size=new(1280,720);await SettleUi();await CaptureSmokeImage("production-convention-720");
            Check(ButtonNamed("Confirm convention booking").GetGlobalRect().End.Y<=_sideScroll.GetGlobalRect().End.Y,"Quote and confirm button fit together at 720p");
            Press("Confirm convention booking");await SettleUi();
            Check(_state.Bookings.Count==1&&_state.Bookings[0].SeriesId==series.Id,"Booking control schedules chosen title and attendee");
            Check(ButtonNamed("Confirm convention booking").Disabled,"Duplicate booth is disabled visibly");
            var save=SaveCareer("Production update");var before=_state.ToJson();LoadCareer(save);await SettleUi();
            Check(_state.ToJson()==before&&_page=="Conventions","Booking view survives save and load");
            OpenOfficeSidebar("Inbox");await SettleUi();Check(!_report.Visible&&!_expanded&&_homeOffice.Visible,"Explicit office Inbox shortcut retains the office");
            GD.Print($"PRODUCTION SMOKE PASSED: {_smokeChecks} checks. First 16-page issue after {(series.Volumes[0].ReleaseDate-start).TotalDays:F1} days with afternoon shifts.");
            var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("PRODUCTION SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
