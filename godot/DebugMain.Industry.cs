using System;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;
using MangakaSim.Catalog;

namespace MangakaGame;

public partial class DebugMain
{
    private RichTextLabel _industryNews = null!, _industryRivals = null!, _industryChannels = null!;
    private Label _industryFeedback = null!, _rivalProfile = null!, _assistantProfile = null!, _offerProfile = null!;
    private OptionButton _rivalChoice = null!, _industryWorkplace = null!, _assistantChoice = null!, _offerChoice = null!, _channelSeries = null!;
    private SpinBox _industrySalary = null!;
    private Button _digitalProposal = null!, _overseasProposal = null!;
    private ItemList _industryFollowers = null!;

    private Control BuildIndustryPanel()
    {
        var scroll = new ScrollContainer { Name = "Industry" };
        var panel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeConstantOverride("separation", 12); scroll.AddChild(panel);
        panel.AddChild(new Label { Text = "Industry and rivals", ThemeTypeVariation = "HeaderLarge" });
        _industryFeedback = IndustryLabel(panel, "");
        _industryNews = IndustryText(panel, 180);
        _industryRivals = IndustryText(panel, 130);
        IndustryLabel(panel, "Rival staff · public specialties and reputation; paid scouting reveals estimates");
        _rivalChoice = IndustryChoice(panel); _rivalChoice.ItemSelected += _ => _dirty = true;
        _rivalProfile = IndustryLabel(panel, "");
        var scouting = new HFlowContainer(); panel.AddChild(scouting);
        IndustryButton(scouting, "Scout · ¥10,000 / 3 days", () => new(TimelineAction.Scout, _rivalChoice.GetSelectedId()));
        IndustryLabel(panel, "Destination workplace and monthly salary · costs use business funds");
        var terms = new HFlowContainer(); panel.AddChild(terms);
        _industryWorkplace = IndustryChoice(terms);
        _industrySalary = new SpinBox { MinValue = StudioRules.MinimumMonthlySalary, MaxValue = 1000000, Step = 1000, Value = 180000 };
        terms.AddChild(_industrySalary);
        IndustryButton(terms, "Approach · ¥20,000 / 7 days", () => new(TimelineAction.Approach, _rivalChoice.GetSelectedId(), _industryWorkplace.GetSelectedId(), (long)_industrySalary.Value));
        IndustryLabel(panel, "Seven days of offered wages and a usable desk are reserved. Approaches can be refused. Salary quotes are per month.");
        IndustryLabel(panel, "Temporary assistants · discover through recruitment, colleagues or the candidate pool");
        _assistantChoice = IndustryChoice(panel); _assistantChoice.ItemSelected += _ => _dirty = true;
        _assistantProfile = IndustryLabel(panel, "");
        var assistants = new HFlowContainer(); panel.AddChild(assistants);
        IndustryButton(assistants, "Hire temporary assistant", () => new(TimelineAction.HireAssistant, _assistantChoice.GetSelectedId(), _industryWorkplace.GetSelectedId(), (long)_industrySalary.Value));
        IndustryLabel(panel, "Recruitment decisions · staff can leave; your own move always needs acceptance");
        _offerChoice = IndustryChoice(panel); _offerChoice.ItemSelected += _ => _dirty = true;
        _offerProfile = IndustryLabel(panel, "");
        var responses = new HFlowContainer(); panel.AddChild(responses);
        IndustryButton(responses, "Offer selected salary to retain", () => new(TimelineAction.Retain, _offerChoice.GetSelectedId(), Salary: (long)_industrySalary.Value));
        IndustryButton(responses, "Withdraw my approach", () => new(TimelineAction.CancelOffer, _offerChoice.GetSelectedId()));
        IndustryButton(responses, "Decline career offer", () => new(TimelineAction.DeclineCareer, _offerChoice.GetSelectedId()));
        IndustryLabel(panel, "Optional colleagues to invite on a career move (Ctrl / click). They must agree; the employer must have desks and funds.");
        _industryFollowers = new ItemList { SelectMode = ItemList.SelectModeEnum.Multi, CustomMinimumSize = new Vector2(0, 90) };
        panel.AddChild(_industryFollowers);
        IndustryButton(responses, "Accept career offer", () => new(TimelineAction.AcceptCareer, _offerChoice.GetSelectedId(), Followers: _industryFollowers.GetSelectedItems().Select(i => (int)_industryFollowers.GetItemMetadata(i)).ToList()));
        IndustryLabel(panel, "Release channels · one overseas market; existing editions keep their earnings attribution");
        _channelSeries = IndustryChoice(panel);
        var channels = new HFlowContainer(); panel.AddChild(channels);
        _digitalProposal = IndustryButton(channels, "Propose digital · ¥30,000", () => new(TimelineAction.RequestDigital, _channelSeries.GetSelectedId()));
        _overseasProposal = IndustryButton(channels, "Propose overseas · ¥100,000", () => new(TimelineAction.RequestOverseas, _channelSeries.GetSelectedId()));
        IndustryLabel(panel, "Publisher review: 7 days, no fee if refused, retry after 90 days. Owner-controlled doujin digital releases open immediately. Setup costs are game estimates.");
        _industryChannels = IndustryText(panel, 150);
        var import = new Button { Text = "Import previous office save (v4)" }; panel.AddChild(import);
        import.Pressed += ImportTimelineSave;
        return scroll;
    }
    private static Label IndustryLabel(Container parent, string text)
    { var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; parent.AddChild(label); return label; }
    private static RichTextLabel IndustryText(Container parent, int height)
    { var text = new RichTextLabel { CustomMinimumSize = new Vector2(0, height), BbcodeEnabled = false }; parent.AddChild(text); return text; }
    private static OptionButton IndustryChoice(Container parent)
    { var choice = new OptionButton { CustomMinimumSize = new Vector2(280, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(choice); return choice; }
    private Button IndustryButton(Container parent, string text, Func<TimelineCommand> command)
    {
        var button = new Button { Text = text }; parent.AddChild(button);
        button.Pressed += () =>
        {
            try { _state.Apply(command()); _industryFeedback.Text = "Done. Check the terms and decisions below."; _dirty = true; ScanEvents(); }
            catch (InvalidCommandException ex) { _industryFeedback.Text = ex.Message; }
        };
        return button;
    }
    private void RefreshIndustry()
    {
        _digitalProposal.Text=$"Propose digital · ¥{_state.ChannelSetupFee(ReleaseChannel.DomesticDigital):N0}";
        _overseasProposal.Text=$"Propose overseas · ¥{_state.ChannelSetupFee(ReleaseChannel.Overseas):N0}";
        void Fill(OptionButton choice, System.Collections.Generic.IEnumerable<(int Id, string Text)> rows)
        {SyncOptions(choice,rows,choice.GetSelectedId());}
        var business = _state.ControlledBusinessId;
        _industryNews.Text = (_state.World.SimulatedFuture ? "SIMULATED FUTURE · historical coverage ended in 2025" : "Historical era · news arrives as events happen") + "\n\n" +
            string.Join("\n", _state.World.News.TakeLast(30).Reverse().Select(n => $"{n.Time:d MMM yyyy} · {(n.Simulated ? "[Simulated] " : "")}{n.Message}"));
        _industryRivals.Text = "Known historical rivals · fictional analogues\n" + string.Join("\n", _state.World.Rivals.Select(r =>
        { var d = TimelineCatalog.Default.Rivals.Single(d => d.Id == r.Key); return $"{d.Title} · {d.Genre} · {_state.PublisherCatalog.Get(d.Magazine).Name} · {r.Phase}"; }));
        Fill(_rivalChoice, _state.People.Where(p => p.Employment is {} job && job.BusinessId != business && _state.World.RivalBusinesses.Contains(job.BusinessId) && !_state.World.Assistants.Any(a => a.PersonId == p.Id)).Select(p => (p.Id, $"{p.Name} · {_state.Businesses.Single(b => b.Id == p.Employment!.BusinessId).Name}")));
        _rivalProfile.Text = _rivalChoice.ItemCount == 0 ? "No rival staff currently available." : _state.RivalStaffProfile(_rivalChoice.GetSelectedId());
        Fill(_industryWorkplace, _state.Locations.Where(l => !l.Closed && l.BusinessId == business).Select(l => (l.Id, $"{l.Name} · {l.District}")));
        Fill(_assistantChoice, _state.World.Assistants.Select((a, i) => (a, i)).Where(x => x.a.DiscoveredBy == business && x.a.PersonId is null && !x.a.Departed && TimelineCatalog.Default.Creators.Single(c => c.Id == x.a.Key).Departure > _state.Clock.Now).Select(x => (x.i, TimelineCatalog.Default.Creators.Single(c => c.Id == x.a.Key).Name)));
        var opportunity = _assistantChoice.ItemCount > 0 ? _state.World.Assistants[_assistantChoice.GetSelectedId()] : null;
        _assistantProfile.Text = opportunity is null ? "No discovered assistant is available. Targeted recruitment improves your chances." : DescribeAssistant(opportunity);
        var activeAssistants=_state.World.Assistants.Where(a=>a.PersonId is {} id&&_state.FindPerson(id)?.Employment?.BusinessId==business&&!a.Departed).ToArray();
        if(activeAssistants.Length>0)_assistantProfile.Text+="\n"+string.Join("\n",activeAssistants.Select(a=>"Currently employed: "+DescribeAssistant(a)));
        Fill(_offerChoice, _state.World.Offers.Where(o => o.Status == NegotiationStatus.Pending && (o.FromBusiness == business || o.ToBusiness == business)).Select(o => (o.Id, $"{_state.FindPerson(o.PersonId)!.Name} · until {o.EndsAt:d MMM}")));
        var offer = _state.World.Offers.FirstOrDefault(o => o.Id == _offerChoice.GetSelectedId());
        _offerProfile.Text = offer is null ? "No pending offers." : $"{_state.FindPerson(offer.PersonId)!.Name} → {_state.Businesses.Single(b => b.Id == offer.ToBusiness).Name}, ¥{offer.Salary:N0}/month, decision {offer.EndsAt:d MMM yyyy HH:mm}. Moving preserves personal savings; business funds stay with each business.";
        var colleagues = _state.ControlledStaff.Where(p => p.Id != _state.ProtagonistPersonId && !_state.World.Assistants.Any(a => a.PersonId == p.Id)).ToArray();
        if (_industryFollowers.ItemCount != colleagues.Length || colleagues.Where((p,i) => i >= _industryFollowers.ItemCount || (int)_industryFollowers.GetItemMetadata(i) != p.Id).Any())
        { _industryFollowers.Clear(); foreach (var p in colleagues) { _industryFollowers.AddItem(p.Name); _industryFollowers.SetItemMetadata(_industryFollowers.ItemCount-1, p.Id); } }
        Fill(_channelSeries, _state.Series.Where(s => s.BusinessId == business && (_state.Control == ControlMode.OwnerDirector || s.LeadPersonId == _state.ProtagonistPersonId)).Select(s => (s.Id, s.Title)));
        _industryChannels.Text = $"Direct doujin downloads: Books → Sell online · no upfront cost.\nPublisher digital agreements: {(_state.ChannelAvailable(ReleaseChannel.DomesticDigital) ? "available" : "not available in this era")} · Overseas: {(_state.ChannelAvailable(ReleaseChannel.Overseas) ? "publisher proposals available" : "not available in this era")}\n" +
            string.Join("\n", _state.World.Channels.Where(a => a.BusinessId == business).Select(a =>
            { var receipts = _state.World.Receipts.Where(r => r.AgreementId == a.Id).ToArray(); return $"{_state.FindSeries(a.SeriesId)!.Title} · {GameState.ChannelName(a.Channel)}: {a.Status} ({a.Reason}) · {receipts.Sum(r => r.Units):N0} channel units / ¥{receipts.Sum(r => r.NetYen):N0} receipts" + (a.Channel == ReleaseChannel.Overseas ? $" · overseas interest {a.InternationalInterest:0}/100" : ""); }));
    }
    private static string DescribeAssistant(CreatorOpportunity a)
    { var d = TimelineCatalog.Default.Creators.Single(c => c.Id == a.Key); return $"{d.Name} · temporary support and mentoring · fixed departure {d.Departure:d MMM yyyy}\nExpected salary ¥{StudioRules.ExpectedSalary(d.Skills.Max()):N0}/month. Promising, uneven skills; no guaranteed prodigy trait. This job opportunity is fictional."; }
    private void ImportTimelineSave()
    {
        if (OfficeEditing) { _industryFeedback.Text = "Finish furnishing before importing."; return; }
        using var file = FileAccess.Open("user://debug-v4.json", FileAccess.ModeFlags.Read);
        if (file is null) { _industryFeedback.Text = "No previous debug-v4.json save found. The original is kept."; return; }
        try
        {
            _state = GameState.ImportTimelineV4(file.GetAsText()); Pause(); _recapDialog.Hide(); _log.Clear(); _scanIndex = _state.Events.Count;
            _selectedPersonId = _state.ProtagonistPersonId; ResetPersonInputs(); _dirty = true;
            _industryFeedback.Text = "Imported at the current date. Save writes debug-v5.json; the original remains intact.";
        }
        catch (Exception ex) when (ex is System.IO.InvalidDataException or JsonException) { _industryFeedback.Text = ex.Message; }
    }
}
