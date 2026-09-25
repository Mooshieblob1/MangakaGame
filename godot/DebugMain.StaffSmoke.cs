using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async Task RunStaffSmoke()
    {
        _state = GameState.NewGame(12, OwnershipMode.CreatorRetention);
        _selectedPersonId = _state.ProtagonistPersonId;
        _scanIndex = 0; _log.Clear(); _recapDialog.Hide(); Pause();
        _mainTabs.CurrentTab = 2; _dirty = true;
        await SettleUi();
        Check(_staffSummary.Text.Contains("Rent ¥0") && _state.PersonalMoney == 200000 && _state.Money == 300000, $"Family home and separate balances shown: {_staffSummary.Text}; personal {_state.PersonalMoney}, business {_state.Money}");
        _contribution.Value = 10000; Press("Contribute savings");
        Check(_state.PersonalMoney == 190000 && _state.Money == 310000, "Contribution control transfers exact amount");
        Press("Recruit — ¥20,000 / 7 days");
        Check(_state.Recruitment is not null && _state.Money == 290000, "Recruitment control starts paid search");
        var candidateId = _candidateOption.GetSelectedId();
        Press("Hire at selected workplace");
        Check(_state.ControlledStaff.Count() == 2 && _state.FindPerson(candidateId)?.Employment is not null, "Hire control adds colleague and employment");
        await SettleUi();
        var before = _state.ToJson(); Press("Hire at selected workplace");
        Check(_state.ToJson() == before && _staffFeedback.Text.Contains("desk"), "Full house rejects hiring with visible reason");
        _selectedPersonId = candidateId; ResetPersonInputs();
        _titleEdit.Text = "Two creators"; SelectGenre(_genreOption,"drama"); _cadenceOption.Selected = (int)Cadence.Monthly;
        Press("Create"); await SettleUi();
        Press("Assign selected staff to team");
        Check(_state.FindPerson(candidateId)!.MainSeriesId == _state.Series.Single().Id, "Team assignment control");
        _personOption.Select(_personOption.GetItemIndex(candidateId));
        _personOption.EmitSignal(OptionButton.SignalName.ItemSelected, _personOption.Selected);
        Check(_startSpin.Value == 9 && _endSpin.Value == 18, "Employee selection updates production schedule");
        await DrivePublishingUntil(() => _state.Clock.Now >= new DateTime(1996, 5, 1));
        Check(_state.FindPerson(candidateId)!.PersonalAccount.Balance > 0, "Payroll pays colleague through real ticks");
        Save(); var saved = _state.ToJson(); AdvanceAndScan(1); Load(); await SettleUi();
        Check(_state.ToJson() == saved && _state.Ownership == OwnershipMode.CreatorRetention, "Staff accounts and ownership save/load exactly");
        _mainTabs.CurrentTab = 2;
        await CaptureSmokeImage("debug-staff");
        Press("Give selected staff 30 days' paid notice");
        Check(_state.FindPerson(candidateId)!.Employment!.NoticeEndsAt is not null, "Dismissal control records notice instead of deleting history");
    }
}
