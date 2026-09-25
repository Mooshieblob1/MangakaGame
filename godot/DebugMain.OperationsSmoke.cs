using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async Task RunOperationsSmoke()
    {
        _state=GameState.NewGame(2);_scanIndex=0;_log.Clear();_recapDialog.Hide();Pause();_dirty=true;
        _selectedPersonId=_state.ProtagonistPersonId;_mainTabs.CurrentTab=3;
        await SettleUi();
        Press("Personal loan");Check(_state.Loans.Count==1&&_state.PersonalMoney==300000,"Personal loan control keeps funds personal");
        await SettleUi();Press("Repay");Check(_state.Loans[0].Principal==90000,"Loan repayment control");
        Press("Four-hour paid commission");await DrivePublishingUntil(()=>_state.Protagonist.RecoveryHours==0);
        Check(_state.Ledger.Any(e=>e.Reason=="recovery commission"),"Recovery control earns through four real hours");
        _titleEdit.Text="Neighbourhood stories";SelectGenre(_genreOption,"drama");_pagesSpin.Value=1;_cadenceOption.Selected=(int)Cadence.Monthly;Press("Create");
        await DrivePublishingUntil(()=>_state.Series[0].Volumes.Count>0);
        var book=_state.Series[0].Volumes[0];Check(book.ReleasedAt is null,"Completed manuscript waits for a print order");
        _printCopies.Value=50;Press("Order copies");Check(_state.PrintRuns.Count==1&&_state.Stock(book.Id)==0,"Printing control pays before delivery");
        await DrivePublishingUntil(()=>_state.Stock(book.Id)>0);Check(book.ReleasedAt is not null,"Delivered stock begins release window");
        Press("Enable for selected title");Check(_state.Series[0].AutoPrint,"Automatic printing control");
        Press("Disable");Check(!_state.Series[0].AutoPrint,"Automatic printing can be stopped");
        Press("Prioritise promotion");Press("14-day campaign — up to ¥5,000");await DrivePublishingUntil(()=>_state.Series[0].CampaignHours>0);
        Check(_state.Series[0].Reach>0,"Campaign uses employee time and builds reach");Press("Stop promotion");
        Press("Book solo");Check(_state.Bookings.Count==1&&_state.Bookings[0].TravelCost==0,"Nearby free convention booking");
        await SettleUi();Press("Cancel booking");Check(_state.Bookings[0].Cancelled,"Convention cancellation control");
        _propertyChoice.Select(0);Press("Move main studio");Check(OfficeEditing,"Management move opens furnishing preview");Press("Apply layout and cost");_mainTabs.CurrentTab=3;Check(_state.Locations.Any(l=>!l.Closed&&l.Seats==4),"Property move control");
        await SettleUi();Press("Add two break seats — ¥10,000");Check(_state.Locations.Single(l=>!l.Closed&&l.BusinessId==_state.ControlledBusinessId).BreakSeats==4,"Break room purchase control");
        Save();var saved=_state.ToJson();_recapDialog.Hide();AdvanceAndScan(1);_recapDialog.Hide();Load();await SettleUi();Check(_state.ToJson()==saved,"Operations save/load through scene controls");
        StudioSection("Overview");await CaptureSmokeImage("debug-operations");
        StudioSection("Career");await CaptureSmokeImage("debug-operations-career");
        _mainTabs.CurrentTab=4;await SettleUi();Check(_travelSummary.Text.Contains("km estimated"),"Tokyo travel map display");await CaptureSmokeImage("debug-tokyo");
        _mainTabs.CurrentTab=3;_recapDialog.Hide();Press("Join employer");
        var confirmation=FindChildren("*","ConfirmationDialog",true,false).OfType<ConfirmationDialog>().Single(d=>d.Visible&&d.Title=="Change career?");
        confirmation.Hide();confirmation.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
        Check(_state.PendingCareer is not null,"Career confirmation schedules day-boundary transition");
        await DrivePublishingUntil(()=>_state.Control==ControlMode.EmployedLead);
        Check(_state.Businesses.Count(b=>!_state.World.RivalBusinesses.Contains(b.Id))==2&&_state.Series[0].BusinessId==_state.ControlledBusinessId,"Career follows the creator and future title");
        Check(book.BusinessId!=_state.ControlledBusinessId,"Released books remain with the former business");
    }
}
