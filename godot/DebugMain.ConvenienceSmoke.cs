using System;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async void RunConvenienceSmoke()
    {
        SetProcess(false);
        try
        {
            Directory.CreateDirectory(SmokeOutput);_careers=new CareerStore(Path.Combine(SmokeOutput,"convenience-careers-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            _state=GameState.NewGame(2);ResetManagementSession();
            _state.Apply(new CreateDoujinCommand("Paper Garden","drama",8));var series=_state.Series[0];
            for(var h=0;h<24*90&&series.Volumes.Count==0;h++)_state.Advance(1);
            Check(series.Volumes.Count==1,"Normal production creates the one-shot");var book=series.Volumes[0];
            _scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;_popupEvents.Clear();_dirty=true;await SettleUi();
            SelectSeriesForWorkbench(series.Id);OpenOnline(series.Id,book.Id);await SettleUi();
            Check(_expanded&&_page=="Sell online","Online selling opens a full management page");
            var money=_state.Money;Press("Publish online · ¥0 upfront");await SettleUi();
            Check(_state.Money==money&&_state.DoujinOnlineListed(book.Id)&&_state.PrintRuns.Count==0,"Online publish lists immediately with no printing or setup debit");
            Check(ButtonNamed("Already on sale online").Disabled,"Duplicate online listing is disabled");
            _state.Advance(24*7);_scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;RefreshManagement();
            Check(_state.DoujinDownloadsSold(book.Id)>0&&_currentFans.Text.Contains($"{series.Fanbase:N0}"),"Purchases add readers and refresh fans in the top bar");
            Check(_currentCopies.Text.Contains($"Sold {_state.DoujinDownloadsSold(book.Id):N0}")&&_currentCopies.Text.Contains("Stock 0"),"Top bar distinguishes downloads sold from physical stock");
            await CaptureSmokeImage("convenience-online");
            var save=SaveCareer("Online shop");LoadCareer(save);await SettleUi();series=_state.Series[0];book=series.Volumes[0];
            Check(_page=="Sell online"&&ButtonNamed("Already on sale online").Disabled,"Loading restores online page and listing");
            var readers=series.Fanbase;Navigate("Series details",series.Id);Press("Continue as ongoing series");await SettleUi();
            Check(!series.StandaloneDoujin&&series.ReleaseShortIssues&&series.Fanbase==readers&&series.Title=="Paper Garden"&&series.Genre=="drama","Continue button preserves title genre and readers");
            Check(_sideContent.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text.Contains("Copies sold")&&l.Text.Contains("downloads")),"Series details display combined sales and physical stock");
            _state.Apply(new StudioActionCommand(StudioAction.Print,book.Id,Amount:50));Navigate("Conventions",series.Id);await SettleUi();
            var reserve=_sideContent.FindChildren("ConventionReservedCopies","SpinBox",true,false).OfType<SpinBox>().Single();reserve.Value=40;
            GetWindow().Size=new(1280,720);await SettleUi();
            Check(!ButtonNamed("Confirm convention booking").Disabled,"Copies in paid print orders can be reserved before delivery");
            await CaptureSmokeImage("convenience-reservation-720");
            Check(ButtonNamed("Confirm convention booking").GetGlobalRect().End.Y<=_sideScroll.GetGlobalRect().End.Y,"Reservation quote and confirmation fit at 720p");
            await CaptureSmokeImage("convenience-reservation-720");Press("Confirm convention booking");await SettleUi();
            var booking=_state.Bookings.Single();Check(booking.ReservedStock.Values.Sum()==40&&booking.Date<=_state.Clock.Now.Date.AddDays(5),"Booking reserves chosen quantity at a nearby upcoming event");
            Check(_currentCopies.Text.Contains("Reserved 40"),"Header shows reserved copies, including pending delivery");
            Check(_shell.GetGlobalRect().End.X<=GetViewportRect().Size.X+1,"Header fits a 1280-pixel window");
            _state.Advance(24);_scanIndex=_state.Events.Count;_lastAutosave=_state.Clock.Now.Date;RefreshManagement();
            Check(_currentCopies.Text.Contains("Stock 50")&&_currentCopies.TooltipText.Contains("Available 10"),"Delivery shows total stock and separates copies free for local sale");
            Navigate("Series");await SettleUi();Check(_sideContent.FindChildren("*","Label",true,false).OfType<Label>().Any(l=>l.Text.Contains("Physical stock 50")&&l.Text.Contains("Convention reserve 40")),"Series list includes current stock and reservations");
            Workbench(0);var unread=UnreadCount();_inboxShortcut.EmitSignal(BaseButton.SignalName.Pressed);await SettleUi();
            Check(_page=="Inbox"&&_side.Visible&&!_report.Visible&&UnreadCount()==unread,"Unread button opens Inbox without marking messages read");
            Check(_inboxShortcut.Text==$"{unread} unread","Unread shortcut carries the actual count");
            _side.Hide();_homeOffice.GrabFocus();SetSpeed(4);
            Check(_speedButtons[4].ButtonPressed&&_speedButtons.Count(x=>x.Value.ButtonPressed)==1,"Exactly the active speed button is highlighted");
            Check(_speedFlash!.Visible&&_speedFlash.Text.Contains("4×")&&_speedFlash.Modulate.A is >0 and <1,"Speed change displays a translucent fullscreen icon");
            Check(_speedFlash.MouseFilter==MouseFilterEnum.Ignore,"Speed feedback does not intercept controls");
            await CaptureSmokeImage("convenience-speed-720");HandleTimeShortcut(Key.Space);
            Check(_speedButtons[0].ButtonPressed&&_speedFlash.Text=="Ⅱ","Keyboard pause updates highlight and overlay");
            HandleTimeShortcut(Key.Key2);Check(_speedButtons[1].ButtonPressed&&_speedFlash.Text.Contains("1×"),"Keyboard speed increase updates feedback");
            await ToSignal(GetTree().CreateTimer(1),SceneTreeTimer.SignalName.Timeout);Check(!_speedFlash.Visible,"Speed feedback fades away automatically");
            Pause();Navigate("Conventions",series.Id);Press("Cancel booking");await SettleUi();Check(_state.ConventionReserved(book.Id)==0,"Cancel action releases reserved copies");
            Navigate("Series details",series.Id);await CaptureSmokeImage("convenience-series-720");
            GD.Print($"CONVENIENCE SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();
        }
        catch(Exception ex){GD.PrintErr("CONVENIENCE SMOKE FAILED: "+ex);GetTree().Quit(1);}
    }
}
