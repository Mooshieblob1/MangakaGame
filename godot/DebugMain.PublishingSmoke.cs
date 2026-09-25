using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private async Task DrivePublishingUntil(Func<bool> condition, int limit = 24 * 730)
    {
        for (var hour = 0; hour < limit && !condition(); hour++)
        {
            AdvanceAndScan(1);
            // The test explicitly acknowledges every recap/offer pause, then
            // continues one real simulation tick at a time without time jumps.
            _recapDialog.Hide();
            if (hour % 128 == 0) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Check(condition(), $"Publishing scenario timed out: {string.Join("; ", _state.Events.TakeLast(6).Select(e => e.Message))}");
        await SettleUi();
    }

    private async Task RestorePublishingBranch(string json)
    {
        _state = GameState.FromJson(json);
        _scanIndex = _state.Events.Count;
        _recapDialog.Hide();
        _dirty = true;
        ResetPersonInputs();
        await SettleUi();
    }

    private async Task RunPublishingSmoke()
    {
        _state = GameState.NewGame(2);
        _scanIndex = 0;
        _log.Clear();
        _recapDialog.Hide();
        _publishingFeedback.Text = "";
        ResetPersonInputs();
        _titleEdit.Text = "Paper Garden";
        SelectGenre(_genreOption,"drama");
        _cadenceOption.Selected = (int)Cadence.Monthly;
        _pagesSpin.Value = 19;
        Press("Create");
        _mainTabs.CurrentTab = 1;
        _magazineOption.Selected = 5;
        await SettleUi();
        _onlineButton.EmitSignal(BaseButton.SignalName.Pressed);
        Check(_state.HasInternet && _state.Money == 180000, "Get online control debits starting cost");
        AdvanceAndScan(1);
        var draft=_state.Series[0].Chapters[0];var progress=draft.Stages.Sum(w=>w.HoursDone);
        Press("Pitch one-shot");
        Check(_state.Series[0].Chapters.Contains(draft)&&draft.Stages.Sum(w=>w.HoursDone)==progress,"Pitch preserves partially drawn work");
        Check(_state.Series[0].Publishing == PublishingStatus.Pitching && _state.Series[0].Chapters.Last().Pages == 31, "Pitch creates 31-page sample without requiring a book");
        await DrivePublishingUntil(() => _state.Series[0].Chapters.Last().Editor == EditorStatus.AwaitingReview);
        Save();
        var reviewSave = _state.ToJson();
        AdvanceAndScan(1);
        Load();
        await SettleUi();
        Check(_state.ToJson() == reviewSave && _publishingSummary.Text.Contains("AwaitingReview"), "Load restores review and display");
        SetSpeed(8);
        await DrivePublishingUntil(() => _state.Series[0].Publishing != PublishingStatus.Pitching);
        Check(_state.Series[0].Publishing == PublishingStatus.Offered && _speed == 0, "Real pitch outcome offers serialization and pauses");
        Check(!_acceptButton.Disabled && !_declineButton.Disabled, "Live offer controls enabled");
        var offered = _state.ToJson();

        Press("Decline offer");
        Check(_state.Series[0].Publishing == PublishingStatus.Unpublished && _state.Series[0].Chapters.Any(c => c.IsOneShot && c.DoujinEligible), "Decline releases sample to doujin");
        await RestorePublishingBranch(offered);
        var expires = _state.Series[0].PendingOffer!.ExpiresAt;
        await DrivePublishingUntil(() => _state.Clock.Now >= expires);
        Check(_state.Series[0].PendingOffer is null && _state.Events.Any(e => e.Type == EventType.OfferExpired), "Ignored offer expires through real ticks");
        await RestorePublishingBranch(offered);
        Press("End series");
        Check(_state.Series[0].Status == SeriesStatus.Ended && !_state.Series[0].Chapters.Any(c => c.IsOneShot), "Ending with offer drops live sample");
        await RestorePublishingBranch(offered);

        Press("Accept offer");
        await SettleUi();
        Check(_state.Series[0].Publishing == PublishingStatus.Serialized && _state.Series[0].Contract is not null, "Accept signs contract");
        var accepted = _state.ToJson();
        Press("Withdraw");
        Check(_state.Series[0].Publishing == PublishingStatus.Unpublished && _state.Series[0].PitchCooldowns.ContainsKey("hoshigaku-flowers"), "Withdraw archives contract and sets cooldown");
        await RestorePublishingBranch(accepted);
        await DrivePublishingUntil(() => _state.Series[0].ChaptersPublished > 0);
        Check(_state.Ledger.Any(e => e.Reason == "chapter fee" && e.Amount > 0), "Published chapter pays fee");
        Check(_state.Markets[5].LastRanking.Any(r => r.SeriesId == _state.Series[0].Id), "Publication appears in magazine ranking");
        await DrivePublishingUntil(() => _state.Series[0].Volumes.Any(v => !v.IsDoujin && v.CopiesSold > 0));
        Check(_state.Ledger.Any(e => e.Reason == "royalties" && e.Amount > 0), "Commercial volume releases and earns royalties");
        Check(_state.Money == _state.ControlledBusiness.Account.OpeningBalance + _state.Ledger.Sum(e => e.Amount), "Displayed balance reconciles with ledger");
        _titleEdit.Text = "Draft input kept";
        _magazineOption.Selected = 5;
        await SettleUi();
        Check(_titleEdit.Text == "Draft input kept" && _magazineOption.Selected == 5, "Refresh preserves input and magazine selection");
        Save();
        var published = _state.ToJson();
        AdvanceAndScan(1);
        Load();
        await SettleUi();
        Check(_state.ToJson() == published && _publishingSummary.Text.Contains("Serialized"), "Publishing save/load restores exact state and controls");
        _publishingFeedback.Text = "Publishing walkthrough: chapter fees and book royalties verified.";
        await CaptureSmokeImage("debug-publishing");
        _marketTabs.CurrentTab = 1;
        await CaptureSmokeImage("debug-books");
        _marketTabs.CurrentTab = 2;
        await CaptureSmokeImage("debug-trends");
        Press("End series");
        Check(_state.Series[0].Status == SeriesStatus.Ended && _state.Series[0].Volumes.Count > 0, "End control preserves published books");
        var ended = _state.ToJson();
        Check(GameState.FromJson(ended).ToJson() == ended, "Ended publishing history validates");
    }
}
