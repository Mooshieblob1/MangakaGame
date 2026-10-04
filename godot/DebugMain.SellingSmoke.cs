using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    // Streaming sales and the selling tutorial (spec 2026-10-03).
    private async void RunSellingSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "selling-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            CheckMergedMoneyLine();
            _state.Apply(new CreateDoujinCommand("Selling pages", "adventure"));
            for (var d = 0; d < 180 && _state.Series[0].Volumes.Count == 0; d++) _state.Advance(24);
            var book = _state.Series[0].Volumes[0]; _state.Series[0].Fanbase = 3000;
            ShowOffice(); RefreshManagement(); UpdateWorkFeedback(.1); await SettleUi();
            Check(SoldPulses == 0 && FirstSaleBubbles == 0, "Looking at a finished, unprinted book announces nothing");
            Check(CareerGuidance.Evaluate(_state, _presentation.Guidance).Id != "sell-more", "A book waiting to be printed is not the selling-more step");
            RefreshGuidance(); OpenPhone(false); await SettleUi();
            Check(_phone.FindChild("GuidanceSellOnline", true, false) is null && _phone.FindChild("GuidanceShowMe", true, false) is Button,
                "Other steps keep the single Show me reply");
            ClosePhone();
            _state.Apply(new StudioActionCommand(StudioAction.Print, book.Id, Amount: 100));
            var pulses = SoldPulses;
            for (var h = 0; h < 48 && _state.Stock(book.Id) == 0; h++) { _state.Advance(1); RefreshManagement(); UpdateWorkFeedback(.1); }
            Check(_state.Stock(book.Id) > 0, "The printed copies were delivered");
            Check(SoldPulses == pulses + 1, "The Sold count pulses when the first copies reach the shops");
            UpdateWorkFeedback(.1);
            Check(SoldPulses == pulses + 1, "Watching again does not pulse a second time");
            var step = CareerGuidance.Evaluate(_state, _presentation.Guidance);
            Check(step.Id == "sell" && step.Text.Contains("sell by themselves"), $"Helper-Chan says the books sell by themselves ({step.Id})");
            Check(step.Target == "sold", $"The step points at Sold ({step.Target})");
            ShowGuidance(); await SettleUi();
            Check(_page == "Office" && !_side.Visible, "Her Show me highlights Sold on the office view");
            Check(_currentCopies.HasFocus() && _currentCopies.FocusMode == Control.FocusModeEnum.All, "The Sold count takes the focus");
            Check(_notice.Text.Contains("10:00 to 20:00"), "She says when the shops sell");
            var stock = _state.Stock(book.Id); var bubbles = FirstSaleBubbles; var money = _state.Money;
            for (var h = 0; h < 24 * 7 && book.CopiesSold == 0; h++) { _state.Advance(1); RefreshManagement(); UpdateWorkFeedback(.1); }
            Check(book.CopiesSold > 0 && _state.Stock(book.Id) < stock && _state.Money > money && SalesRules.IsShopHour(_state.Clock.Now),
                $"Copies sell during shop hours ({book.CopiesSold} sold at {_state.Clock.Now:HH:mm})");
            Check(FirstSaleBubbles == bubbles + 1, "The first copy sold gets its bubble");
            Check(GetChildren().OfType<Label>().Any(l => l.Name == "StageBubble" && l.Text == "First copy sold!"), "The bubble is on screen with its words");
            for (var i = 0; i < 6; i++) { _state.Advance(1); RefreshManagement(); UpdateWorkFeedback(.1); }
            Check(FirstSaleBubbles == bubbles + 1, "Later copies do not repeat the first-sale bubble");
            await CaptureSmokeImage("selling-first-sale");
            step = CareerGuidance.Evaluate(_state, _presentation.Guidance);
            Check(step.Id == "sell-more", $"Helper-Chan then suggests selling more ({step.Id})");
            RefreshGuidance(); OpenPhone(false); await SettleUi();
            Check(_phone.FindChild("GuidanceSellOnline", true, false) is Button && _phone.FindChild("GuidanceConventions", true, false) is Button,
                "Her sell-more texts offer Sell online and Conventions");
            Check(_phone.FindChild("GuidanceShowMe", true, false) is null, "The plain Show me reply is not offered beside them");
            Press("Sell online"); await SettleUi();
            Check(_page == "Sell online", "The Sell online reply opens Sell online");
            OpenPhone(false); await SettleUi();
            Press("Conventions"); await SettleUi();
            Check(_page == "Conventions", "The Conventions reply opens Conventions");

            _presentation.ReducedUiMotion = true; ShowOffice(); await SettleUi();
            PulseSold();
            Check(_currentCopies.Modulate.IsEqualApprox(new Color(BrandPalette.Gold)), "With reduced motion the Sold count is highlighted, not pulsed");
            _hadSale = false; _salesKey = null; ObserveSales(); await SettleUi();
            Check(_notice.Text.Contains("First copy sold!"), "With reduced motion the first sale is a notice line, not a bubble");
            _presentation.ReducedUiMotion = false;

            // Loading a career is a starting point: nothing already on sale pulses and no old sale gets a bubble.
            pulses = SoldPulses; bubbles = FirstSaleBubbles;
            ResetSalesWatch(); ObserveSales();
            Check(SoldPulses == pulses && FirstSaleBubbles == bubbles, "A first look at a career with sales announces nothing");

            // Final review fix 2: the watcher runs every frame, so it looks through the books only when the simulation moved.
            var looks = SalesRecomputes; UpdateWorkFeedback(.1); UpdateWorkFeedback(.1); UpdateWorkFeedback(.1);
            Check(SalesRecomputes == looks, "With the clock still, the Sold watcher does not look through the books again");
            _state.Advance(1); UpdateWorkFeedback(.1);
            Check(SalesRecomputes == looks + 1, "Once the clock moves it looks once more");

            // Final review fix 5: a move into another business is a first look too, so a title it already sells neither
            // pulses Sold nor gets "First copy sold!".
            var home = _state.ControlledBusinessId; var other = _state.Businesses.Max(b => b.Id) + 1000; var title = _state.Series[0];
            title.BusinessId = other; ResetSalesWatch(); ObserveSales(); // the old business: nothing on sale, no sale yet
            pulses = SoldPulses; bubbles = FirstSaleBubbles;
            _state.ControlledBusinessId = other; _salesKey = null; ObserveSales(); // the move lands on a tick, so the watcher looks again
            Check(SoldPulses == pulses && FirstSaleBubbles == bubbles, "A move into a business that already sells a title announces nothing");
            _state.ControlledBusinessId = home; title.BusinessId = home; ResetSalesWatch(); ObserveSales();

            // Final review fix 3: leave the way a player does after a session with sales, Quit to title and then Quit.
            ShowMenu(); await SettleUi(); Press("Quit to title");
            for (var f = 0; f < 300 && !TitleOpen; f++) await SettleUi();
            Check(TitleOpen, "Quit to title reaches the title screen");
            GD.Print($"SELLING SMOKE PASSED: {_smokeChecks} checks.");
            Press("Quit"); // the title's Quit button: QuitGame, so the runner sees exit code 0
        }
        catch (Exception ex) { GD.PushError($"SELLING SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    // Streaming sales add to today's ledger line instead of adding one, so the header money must notice balance changes alone.
    private void CheckMergedMoneyLine()
    {
        var account = new CashAccount { OpeningBalance = 100, Balance = 100 }; var feedback = new CashFeedback();
        account.Entries.Add(new(_state.Clock.Now, 100, "doujin sales", null)); feedback.Observe(account);
        Check(feedback.Gains == 0, "The first look at an account with entries shows no gain");
        account.Balance += 30; account.Entries[0] = account.Entries[0] with { Amount = 130 }; feedback.Observe(account);
        Check(feedback.Gains == 30 && feedback.GainReason == "sales", $"Income merged into today's line still flashes ({feedback.Gains})");
        account.Entries.Add(new(_state.Clock.Now, -20, "printing", null)); account.Balance -= 20; feedback.Observe(account);
        Check(feedback.Gains == 30 && feedback.Losses == 20, "A new entry is counted once, not again as a balance change");
        account.Balance += 5; account.Entries[0] = account.Entries[0] with { Amount = 135 }; account.Entries.Add(new(_state.Clock.Now, 10, "prize", null)); account.Balance += 10; feedback.Observe(account);
        Check(feedback.Gains == 45, $"A merged increase beside a new entry adds both ({feedback.Gains})");
    }
}
