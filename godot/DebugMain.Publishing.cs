using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    private TabContainer _mainTabs = null!;
    private TabContainer _marketTabs = null!;
    private OptionButton _publishingSeries = null!;
    private OptionButton _magazineOption = null!;
    private Label _studioSummary = null!;
    private Label _publishingSummary = null!;
    private Label _publishingFeedback = null!;
    private Label _magazineSummary = null!;
    private Tree _rankingTree = null!;
    private Tree _volumeTree = null!;
    private Tree _trendTree = null!;
    private RichTextLabel _ledgerText = null!;
    private Button _pitchButton = null!;
    private Button _acceptButton = null!;
    private Button _declineButton = null!;
    private Button _withdrawButton = null!;
    private Button _endButton = null!;
    private Button _onlineButton = null!;

    private Control BuildPublishingPanel()
    {
        var panel = new VBoxContainer { Name = "Publishing" };
        panel.AddThemeConstantOverride("separation", 10);
        _studioSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_studioSummary);
        var selections = new HFlowContainer();
        selections.AddChild(new Label { Text = "Series" });
        _publishingSeries = new OptionButton { CustomMinimumSize = new Vector2(260, 0) };
        _publishingSeries.ItemSelected += _ => _dirty = true;
        selections.AddChild(_publishingSeries);
        selections.AddChild(new Label { Text = "Magazine" });
        _magazineOption = new OptionButton { CustomMinimumSize = new Vector2(310, 0) };
        foreach (var magazine in _state.PublisherCatalog.Magazines) _magazineOption.AddItem(magazine.Name);
        _magazineOption.ItemSelected += _ => _dirty = true;
        selections.AddChild(_magazineOption);
        panel.AddChild(selections);
        var actions = new HFlowContainer();
        Button Action(string name, Action action)
        {
            var button = new Button { Text = name };
            button.Pressed += action;
            actions.AddChild(button);
            return button;
        }
        _pitchButton = Action("Pitch one-shot", () => PublishingCommand(id => new PitchSeriesCommand(id, SelectedMagazineId())));
        _acceptButton = Action("Accept offer", () => ConfirmPublishing("Accept publishing offer",id => new AcceptOfferCommand(id)));
        _declineButton = Action("Decline offer", () => ConfirmPublishing("Decline publishing offer",id => new DeclineOfferCommand(id)));
        _withdrawButton = Action("Withdraw", () => ConfirmPublishing("Withdraw from serialization",id => new WithdrawSeriesCommand(id)));
        _endButton = Action("End series", () => ConfirmPublishing("End this series",id => new EndSeriesCommand(id)));
        _onlineButton = Action("Get online", () => TryApply(new GetOnlineCommand()));
        panel.AddChild(actions);
        _publishingFeedback = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_publishingFeedback);
        _publishingSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        panel.AddChild(_publishingSummary);
        _marketTabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        panel.AddChild(_marketTabs);

        var ranking = new VBoxContainer { Name = "Magazine rankings" };
        _magazineSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        ranking.AddChild(_magazineSummary);
        _rankingTree = Table("Rank", "Series", "Score", "Studio / roster");
        ranking.AddChild(_rankingTree);
        _marketTabs.AddChild(ranking);

        var books = new VBoxContainer { Name = "Books and ledger" };
        _volumeTree = Table("Volume", "Channel", "Chapters", "Release", "Copies sold", "Selling weeks");
        books.AddChild(_volumeTree);
        books.AddChild(new Label { Text = "Recent transactions (yen)" });
        _ledgerText = new RichTextLabel { SizeFlagsVertical = SizeFlags.ExpandFill, SelectionEnabled = true };
        books.AddChild(_ledgerText);
        _marketTabs.AddChild(books);

        _trendTree = Table("Genre", "Popularity", "Studio influence", "Noise", "Boom");
        _trendTree.Name = "Genre trends";
        _marketTabs.AddChild(_trendTree);
        return panel;
    }

    private static Tree Table(params string[] columns)
    {
        var tree = new Tree { Columns = columns.Length, ColumnTitlesVisible = true, HideRoot = true,
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        for (var i = 0; i < columns.Length; i++) tree.SetColumnTitle(i, columns[i]);
        return tree;
    }
    private static TreeItem Row(Tree tree, TreeItem root, params string[] values)
    {
        var item = tree.CreateItem(root);
        for (var i = 0; i < values.Length; i++) item.SetText(i, values[i]);
        return item;
    }
    private string SelectedMagazineId() => _state.PublisherCatalog.Magazines[Math.Max(0, _magazineOption.Selected)].Id;
    private void PublishingCommand(Func<int, ICommand> command)
    {
        if (_publishingSeries.ItemCount == 0) { LogLine("Command rejected: no series selected."); return; }
        TryApply(command(_publishingSeries.GetSelectedId()));
    }
    private void ConfirmPublishing(string title,Func<int,ICommand> command)
    {
        if(!ManagementInterface){PublishingCommand(command);return;}
        var series=_state.FindSeries(_publishingSeries.GetSelectedId());if(series is null)return;
        var request=command(series.Id);
        ConfirmPlayerAction(title,series.Title+"\n"+PublishingStatusText(series)+"\n\n"+(request is AcceptOfferCommand?"Accepting begins publisher deadlines under these terms.":"This changes the title's publishing status. Existing books and their recorded sales are retained."),()=>TryApply(request));
    }
    private void RefreshPublishing()
    {
        var selected = _publishingSeries.ItemCount == 0 ? -1 : _publishingSeries.GetSelectedId();
        // Follow the current series, so a newly created title is the one offered for pitching.
        if (ManagementInterface && _progressSeriesId > 0) selected = _progressSeriesId;
        SyncOptions(_publishingSeries,_state.Series.Where(s=>!ManagementInterface||s.BusinessId==_state.ControlledBusinessId&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).Select(s=>(s.Id,s.Title)),selected);
        var series = _publishingSeries.ItemCount == 0 ? null : _state.FindSeries(_publishingSeries.GetSelectedId());
        var magazine = _state.PublisherCatalog.Get(SelectedMagazineId());
        var market = _state.Markets.Single(m => m.MagazineId == magazine.Id);
        _studioSummary.Text = $"Business ¥{_state.Money:N0} • Personal ¥{_state.PersonalMoney:N0} • Price index {Economy.PriceIndex(_state.TrendCatalog, _state.Clock.Now):F3} • " +
            $"Track record {_state.StudioTrackRecord:F1}   •   Staff reputation {_state.StaffReputation:F1}   •   Effective reputation {_state.EffectiveReputation:F1}   •   " +
            (_state.HasInternet ? "Online" : "Offline");
        _onlineButton.Text = _state.HasInternet ? "Online" : $"Get online (¥{Economy.InternetCost(_state.TrendCatalog, _state.Clock.Now):N0})";
        _onlineButton.Disabled = _state.HasInternet || _state.AvailableBusinessCash < Economy.InternetCost(_state.TrendCatalog, _state.Clock.Now);
        _pitchButton.Disabled = series is null || series.Status != SeriesStatus.Active || series.Publishing != PublishingStatus.Unpublished || series.StandaloneDoujin;
        _acceptButton.Disabled = _declineButton.Disabled = series?.Publishing != PublishingStatus.Offered;
        _withdrawButton.Disabled = series?.Publishing != PublishingStatus.Serialized;
        _endButton.Disabled = series is null || series.Status == SeriesStatus.Ended;
        if (series is null) _publishingSummary.Text = "Create a series in Production, then build a doujin audience or pitch a one-shot here.";
        else
        {
            var chapter = series.Chapters.LastOrDefault();
            var quality = series.Chapters.LastOrDefault(c => c.Quality is not null)?.Quality;
            var contract = series.Contract;
            var offer = series.PendingOffer;
            var lines = new List<string>
            {
                series.StandaloneDoujin?"This is a self-published one-shot, not a magazine sample. Use Continue as ongoing series in its Series details to seek serialization.":"Pitching creates a separate 31-page magazine sample. Unfinished doujin chapters are preserved on hold. No completed book is required. Only an accepted serialization has publisher deadline penalties.",
                $"{series.Title} — {series.Publishing}, {series.Status}{(series.IsIconic ? ", ICONIC" : "")}   •   Fans {series.Fanbase:N0}   •   Impact {series.CulturalImpact:F1}   •   Published {series.ChaptersPublished}",
                $"Last quality {quality?.ToString() ?? "—"}   •   Rank {series.LastRank?.ToString() ?? "—"}   •   Strikes {series.Strikes.Count}   •   Protection {_state.Protection(series):P0}" +
                    (series.WarningIssuedAt is { } warning ? $"   •   WARNING since {warning:d MMM yyyy}" : ""),
            };
            if (contract is not null) lines.Add($"Contract: {_state.PublisherCatalog.Get(contract.MagazineId).Name}, ¥{contract.FeePerPage:N0}/page; cancellation line #{_state.PublisherCatalog.Get(contract.MagazineId).CancellationRank}.");
            if (offer is not null) lines.Add($"Offer: {_state.PublisherCatalog.Get(offer.MagazineId).Name}, ¥{offer.FeePerPage:N0}/page; first issue / expiry {offer.FirstIssueClose:d MMM yyyy HH:mm}.");
            if (chapter is not null) lines.Add($"Current ch.{chapter.Number}, {chapter.Pages} pages: {chapter.Status}; editor {chapter.Editor}, redos {chapter.RedoCount}" +
                (chapter.EditorDecisionAt is { } decision ? $"; decision {decision:d MMM yyyy HH:mm}" : "") + $"; {ProductionTarget(chapter)}.");
            if (series.PitchCooldowns.TryGetValue(magazine.Id, out var until) && until > _state.Clock.Now) lines.Add($"This magazine accepts another pitch after {until:d MMM yyyy}.");
            if (!series.StandaloneDoujin && series.Publishing == PublishingStatus.Unpublished)
                lines.Add($"Helper-Chan's estimate for a pitch to this magazine: {CareerGuidance.Outlook(_state, series, magazine.Id).Chance:P0} chance.");
            _publishingSummary.Text = string.Join("\n", lines);
        }
        _magazineSummary.Text = $"{magazine.Name}   •   Tier {magazine.Tier} / {magazine.Demographic}   •   Close {market.NextIssueClose:ddd d MMM yyyy HH:mm}   •   Cancellation line #{magazine.CancellationRank}";
        _rankingTree.Clear();
        var rankingRoot = _rankingTree.CreateItem();
        foreach (var rank in market.LastRanking)
        {
            var item = Row(_rankingTree, rankingRoot, rank.Rank.ToString(), rank.Title, rank.Score.ToString("F1"), rank.SeriesId is null ? "Roster" : "YOUR STUDIO");
            if (rank.SeriesId is not null) for (var i = 0; i < 4; i++) item.SetCustomColor(i, new Color("79ddb0"));
        }
        _volumeTree.Clear();
        var volumeRoot = _volumeTree.CreateItem();
        if (series is not null) foreach (var volume in series.Volumes)
            Row(_volumeTree, volumeRoot, volume.Number.ToString(), volume.IsDoujin ? "Doujin" : "Tankobon", $"{volume.FirstChapter}–{volume.LastChapter}",
                volume.ReleaseDate.ToString("yyyy-MM-dd"), volume.CopiesSold.ToString("N0"), $"{volume.WeeksOnSale}/{volume.SalesWindowWeeks}" + (volume.SalesClosed ? " (closed)" : ""));
        _ledgerText.Text = string.Join("\n", _state.Ledger.TakeLast(25).Reverse().Select(e =>
            $"{e.Time:yyyy-MM-dd}   {e.Amount:+#,##0;-#,##0;0}   {e.Reason}   {(e.SeriesId is { } id ? _state.FindSeries(id)?.Title : "Studio")}"));
        _trendTree.Clear();
        var trendRoot = _trendTree.CreateItem();
        foreach (var trend in _state.Trends) Row(_trendTree, trendRoot, trend.Genre, _state.GenrePopularity(trend.Genre).ToString("F2"),
            trend.PlayerInfluence.ToString("F2"), trend.Noise.ToString("F3"), trend.Boom.ToString("F2"));
    }
}
