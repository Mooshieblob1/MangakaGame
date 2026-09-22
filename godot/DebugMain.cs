using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>
/// Debug harness for the simulation core: clock and speed controls, a chapter table,
/// a person panel with queue editing, a market column (rankings, ledger, volumes),
/// an event log, and save/load. All UI is built in code.
/// </summary>
public partial class DebugMain : Control
{
    private const double SecondsPerHourAt1x = 2.5;
    private string _savePath = "user://debug.json";
    private const int LogLinesOnLoad = 200;
    private const int LedgerTailLines = 8;

    private GameState _state = GameState.NewGame();
    private double _speed;            // 0 = paused
    private double _resumeSpeed = 1;  // speed to return to after an auto-pause
    private double _accumulator;
    private int _scanIndex;
    private bool _dirty = true;

    private Label _clockLabel = null!;
    private Label _speedLabel = null!;
    private RichTextLabel _log = null!;
    private AcceptDialog _recapDialog = null!;

    private LineEdit _titleEdit = null!;
    private LineEdit _genreEdit = null!;
    private OptionButton _cadenceOption = null!;
    private SpinBox _pagesSpin = null!;
    private OptionButton _seriesOption = null!;
    private OptionButton _setCadenceOption = null!;
    private SpinBox _setPagesSpin = null!;
    private OptionButton _magazineOption = null!;
    private Label _seriesStatusLabel = null!;

    private GridContainer _chapterGrid = null!;
    private Label _studioLabel = null!;
    private Label _trendsLabel = null!;

    private OptionButton _personOption = null!;
    private Label _personLabel = null!;
    private Label _moodLabel = null!;
    private SpinBox _salarySpin = null!;
    private readonly Dictionary<Stage, CheckBox> _stageBoxes = new();
    private OptionButton _promotionOption = null!;
    private VBoxContainer _hiringBox = null!;
    private Label _studioPanelLabel = null!;
    private HFlowContainer _premisesRow = null!;
    private HFlowContainer _amenityRow = null!;
    private SpinBox _startSpin = null!;
    private SpinBox _endSpin = null!;
    private readonly Dictionary<DayOfWeek, CheckBox> _dayOffBoxes = new();
    private CheckBox _overtimeBox = null!;
    private VBoxContainer _queueBox = null!;

    private Label _rankingTitle = null!;
    private RichTextLabel _rankingText = null!;
    private RichTextLabel _ledgerText = null!;
    private RichTextLabel _volumesText = null!;

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{edge}", 12);
        AddChild(margin);
        var root = new HBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        root.AddChild(BuildLeftColumn());
        root.AddChild(BuildPersonColumn());
        root.AddChild(BuildMarketColumn());
        root.AddChild(BuildLogColumn());

        _recapDialog = new AcceptDialog { Title = "Daily recap", OkButtonText = "Continue", Exclusive = true };
        _recapDialog.Confirmed += OnRecapContinue;
        AddChild(_recapDialog);

        _scanIndex = 0;
        foreach (var ev in _state.Events) AppendLog(ev);
        _scanIndex = _state.Events.Count;
        ResetPersonInputs();
        Refresh();
        if (OS.GetCmdlineUserArgs().Contains("--smoke-test")) CallDeferred(nameof(RunSmokeTest));
    }

    public override void _Process(double delta)
    {
        if (_speed > 0)
        {
            _accumulator += delta * _speed / SecondsPerHourAt1x;
            var whole = (int)Math.Floor(_accumulator);
            if (whole > 0)
            {
                _accumulator -= whole;
                AdvanceAndScan(whole);
            }
        }

        if (_dirty) Refresh();
    }

    // ---------------------------------------------------------------- simulation driving

    private bool AdvanceAndScan(int hours)
    {
        // Stop at the exact tick that auto-pauses, even after a slow rendering frame.
        for (var i = 0; i < hours; i++)
        {
            _state.Advance(1);
            _dirty = true;
            if (ScanEvents()) return true;
        }
        return false;
    }

    private bool ScanEvents()
    {
        var fresh = _state.EventsSince(_scanIndex).ToList();
        _scanIndex = _state.Events.Count;

        GameEvent? recap = null;
        var shouldPause = false;
        foreach (var ev in fresh)
        {
            AppendLog(ev);
            if (ev.Type == EventType.DailyRecap ||
                (_state.Settings.AutoPause.TryGetValue(ev.Type, out var pause) && pause))
            {
                shouldPause = true;
                if (ev.Type == EventType.DailyRecap) recap = ev;
            }
        }

        if (shouldPause) Pause();
        if (recap != null) ShowRecap(recap);
        return shouldPause;
    }

    private void SetSpeed(double speed)
    {
        if (speed > 0) _resumeSpeed = speed;
        _speed = speed;
        _accumulator = 0;
        _dirty = true;
    }

    private void Pause()
    {
        if (_speed > 0) _resumeSpeed = _speed;
        _speed = 0;
        _accumulator = 0;
        _dirty = true;
    }

    private void ShowRecap(GameEvent ev)
    {
        var lines = new List<string> { ev.Message };
        if (ev.Recap is { } recap)
        {
            foreach (var h in recap.HoursPerPerson)
                lines.Add($"{h.Name}: {h.Hours}h worked, {h.OvertimeHours}h overtime");
            foreach (var s in recap.StagesStarted) lines.Add($"Started: {DescribeChapter(s.SeriesId, s.ChapterNumber, s.Stage)}");
            foreach (var s in recap.StagesCompleted) lines.Add($"Completed: {DescribeChapter(s.SeriesId, s.ChapterNumber, s.Stage)}");
            foreach (var c in recap.ChaptersCompleted) lines.Add($"Chapter done: {DescribeChapter(c.SeriesId, c.ChapterNumber)}");
            foreach (var c in recap.DeadlinesMissed) lines.Add($"Deadline missed: {DescribeChapter(c.SeriesId, c.ChapterNumber)}");
            foreach (var c in recap.ChaptersAtRisk) lines.Add($"At risk: {DescribeChapter(c.SeriesId, c.ChapterNumber)}");
            if (recap.ChaptersPublished > 0) lines.Add($"Chapters published: {recap.ChaptersPublished}");
            if (recap.IssuesMissed > 0) lines.Add($"Issues missed: {recap.IssuesMissed}");
            if (recap.YenEarned != 0) lines.Add($"Yen earned: {recap.YenEarned:N0}");
            if (recap.YenSpent != 0) lines.Add($"Yen spent: {recap.YenSpent:N0}");
            foreach (var m in recap.Moods)
                lines.Add($"{m.Name}: happiness {m.Happiness:0}, fatigue {m.Fatigue:0}, breaks {m.Breaks}{(m.IsMoonlighting ? ", moonlighting" : "")}");
        }
        _recapDialog.DialogText = string.Join("\n", lines);
        _recapDialog.PopupCentered();
    }

    private void OnRecapContinue()
    {
        _recapDialog.Hide();
        if (!AdvanceAndScan(_state.HoursUntilNextWork())) SetSpeed(_resumeSpeed);
    }

    private void TryApply(ICommand command)
    {
        try
        {
            _state.Apply(command);
            _dirty = true;
            ScanEvents();
        }
        catch (InvalidCommandException ex)
        {
            LogLine($"Command rejected: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------- save / load

    private void Save()
    {
        var json = _state.ToJson();
        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            LogLine($"Save failed: {FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(json);
        file.Flush();
        if (file.GetError() != Error.Ok)
        {
            LogLine($"Save failed: {file.GetError()}");
            return;
        }
        LogLine($"Saved to {ProjectSettings.GlobalizePath(_savePath)}");
    }

    private void Load()
    {
        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            LogLine($"Load failed: {FileAccess.GetOpenError()}");
            return;
        }

        GameState loaded;
        try
        {
            loaded = GameState.FromJson(file.GetAsText());
        }
        catch (Exception ex) when (ex is System.IO.InvalidDataException or JsonException)
        {
            LogLine($"Load failed: {ex.Message}");
            return;
        }

        _state = loaded;
        _recapDialog.Hide();
        Pause();
        _log.Clear();
        foreach (var ev in _state.Events.TakeLast(LogLinesOnLoad)) AppendLog(ev);
        _scanIndex = _state.Events.Count;
        LogLine("Loaded.");
        RefreshPersonOption();
        ResetPersonInputs();
        _dirty = true;
        var recap = _state.Events.LastOrDefault(e => e.Type == EventType.DailyRecap);
        if (_state.RecapFiredToday && recap?.Time == _state.Clock.Now) ShowRecap(recap);
    }

    // ---------------------------------------------------------------- UI construction

    private Control BuildLeftColumn()
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 2.6f,
        };

        _clockLabel = new Label();
        column.AddChild(_clockLabel);

        var speeds = new HBoxContainer();
        _speedLabel = new Label();
        speeds.AddChild(_speedLabel);
        foreach (var (label, speed) in new[] { ("Pause", 0.0), ("1x", 1.0), ("2x", 2.0), ("4x", 4.0), ("8x", 8.0) })
        {
            var button = new Button { Text = label };
            button.Pressed += () => SetSpeed(speed);
            speeds.AddChild(button);
        }
        var save = new Button { Text = "Save" };
        save.Pressed += Save;
        speeds.AddChild(save);
        var load = new Button { Text = "Load" };
        load.Pressed += Load;
        speeds.AddChild(load);
        column.AddChild(speeds);

        column.AddChild(BuildSeriesForm());
        column.AddChild(BuildSeriesControls());
        column.AddChild(BuildPublishingControls());
        _seriesStatusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_seriesStatusLabel);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _chapterGrid = new GridContainer { Columns = 12, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_chapterGrid);
        column.AddChild(scroll);

        _studioLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_studioLabel);
        _trendsLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_trendsLabel);
        return column;
    }

    private Control BuildSeriesForm()
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "New series:" });
        _titleEdit = new LineEdit { PlaceholderText = "Title", CustomMinimumSize = new Vector2(140, 0) };
        row.AddChild(_titleEdit);
        _genreEdit = new LineEdit { PlaceholderText = "Genre", CustomMinimumSize = new Vector2(100, 0) };
        row.AddChild(_genreEdit);
        _cadenceOption = MakeCadenceOption();
        row.AddChild(_cadenceOption);
        _pagesSpin = new SpinBox { MinValue = 1, MaxValue = 200, Value = 19, Step = 1 };
        row.AddChild(_pagesSpin);
        var create = new Button { Text = "Create" };
        create.Pressed += () => TryApply(new CreateSeriesCommand(
            _titleEdit.Text, _genreEdit.Text, (Cadence)_cadenceOption.GetSelectedId(), (int)_pagesSpin.Value));
        row.AddChild(create);
        return row;
    }

    private Control BuildSeriesControls()
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "Series:" });
        _seriesOption = new OptionButton { CustomMinimumSize = new Vector2(140, 0) };
        _seriesOption.ItemSelected += _ => _dirty = true;
        row.AddChild(_seriesOption);

        var pause = new Button { Text = "Pause" };
        pause.Pressed += () => WithSelectedSeries(id => TryApply(new PauseSeriesCommand(id)));
        row.AddChild(pause);
        var resume = new Button { Text = "Resume" };
        resume.Pressed += () => WithSelectedSeries(id => TryApply(new ResumeSeriesCommand(id)));
        row.AddChild(resume);

        _setCadenceOption = MakeCadenceOption();
        row.AddChild(_setCadenceOption);
        var setCadence = new Button { Text = "Set cadence" };
        setCadence.Pressed += () => WithSelectedSeries(id =>
            TryApply(new SetCadenceCommand(id, (Cadence)_setCadenceOption.GetSelectedId())));
        row.AddChild(setCadence);

        _setPagesSpin = new SpinBox { MinValue = 1, MaxValue = 200, Value = 19, Step = 1 };
        row.AddChild(_setPagesSpin);
        var setPages = new Button { Text = "Set pages" };
        setPages.Pressed += () => WithSelectedSeries(id => TryApply(new SetPagesPerChapterCommand(id, (int)_setPagesSpin.Value)));
        row.AddChild(setPages);
        return row;
    }

    private Control BuildPublishingControls()
    {
        // A flow container wraps the buttons instead of forcing the whole window wider.
        var row = new HFlowContainer();
        row.AddChild(new Label { Text = "Publishing:" });
        _magazineOption = new OptionButton { CustomMinimumSize = new Vector2(190, 0) };
        var index = 0;
        foreach (var magazine in _state.Publishers.Magazines)
            _magazineOption.AddItem($"{magazine.Name} (T{magazine.Tier} {magazine.Cadence})", index++);
        _magazineOption.Selected = 0;
        _magazineOption.ItemSelected += _ => _dirty = true;
        row.AddChild(_magazineOption);

        var pitch = new Button { Text = "Pitch" };
        pitch.Pressed += () => WithSelectedSeries(id => TryApply(new PitchSeriesCommand(id, SelectedMagazine().Id)));
        row.AddChild(pitch);
        var accept = new Button { Text = "Accept" };
        accept.Pressed += () => WithSelectedSeries(id => TryApply(new AcceptOfferCommand(id)));
        row.AddChild(accept);
        var decline = new Button { Text = "Decline" };
        decline.Pressed += () => WithSelectedSeries(id => TryApply(new DeclineOfferCommand(id)));
        row.AddChild(decline);
        var withdraw = new Button { Text = "Withdraw" };
        withdraw.Pressed += () => WithSelectedSeries(id => TryApply(new WithdrawSeriesCommand(id)));
        row.AddChild(withdraw);
        var end = new Button { Text = "End series" };
        end.Pressed += () => WithSelectedSeries(id => TryApply(new EndSeriesCommand(id)));
        row.AddChild(end);
        return row;
    }

    private Magazine SelectedMagazine() => _state.Publishers.Magazines[Math.Max(0, _magazineOption.Selected)];

    private static OptionButton MakeCadenceOption()
    {
        var option = new OptionButton();
        foreach (var cadence in Enum.GetValues<Cadence>()) option.AddItem(cadence.ToString(), (int)cadence);
        option.Selected = 0;
        return option;
    }

    private Control BuildPersonColumn()
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.2f,
        };

        var head = new HBoxContainer();
        head.AddChild(new Label { Text = "Staff:" });
        _personOption = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
        _personOption.ItemSelected += _ => { ResetPersonInputs(); _dirty = true; };
        head.AddChild(_personOption);
        var fire = new Button { Text = "Fire" };
        fire.Pressed += () => TryApply(new FireCommand(SelectedPerson().Id));
        head.AddChild(fire);
        column.AddChild(head);

        _personLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_personLabel);
        _moodLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_moodLabel);

        var hours = new HBoxContainer();
        hours.AddChild(new Label { Text = "Start" });
        _startSpin = new SpinBox { MinValue = 0, MaxValue = 23, Step = 1 };
        hours.AddChild(_startSpin);
        hours.AddChild(new Label { Text = "End" });
        _endSpin = new SpinBox { MinValue = 1, MaxValue = 24, Step = 1 };
        hours.AddChild(_endSpin);
        column.AddChild(hours);

        var days = new HFlowContainer();
        days.AddChild(new Label { Text = "Days off:" });
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var box = new CheckBox { Text = day.ToString()[..3] };
            _dayOffBoxes[day] = box;
            days.AddChild(box);
        }
        column.AddChild(days);

        var apply = new Button { Text = "Apply schedule" };
        apply.Pressed += () =>
        {
            var person = SelectedPerson();
            var daysOff = _dayOffBoxes.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToHashSet();
            TryApply(new SetScheduleCommand(person.Id, (int)_startSpin.Value, (int)_endSpin.Value, daysOff));
        };
        column.AddChild(apply);

        _overtimeBox = new CheckBox { Text = "Overtime allowed" };
        _overtimeBox.Toggled += on => TryApply(new SetOvertimeAllowedCommand(SelectedPerson().Id, on));
        column.AddChild(_overtimeBox);

        var pay = new HBoxContainer();
        pay.AddChild(new Label { Text = "Salary" });
        _salarySpin = new SpinBox { MinValue = 0, MaxValue = 5_000_000, Step = 1000, CustomMinimumSize = new Vector2(110, 0) };
        pay.AddChild(_salarySpin);
        var setSalary = new Button { Text = "Set salary" };
        setSalary.Pressed += () => TryApply(new SetSalaryCommand(SelectedPerson().Id, (int)_salarySpin.Value));
        pay.AddChild(setSalary);
        column.AddChild(pay);

        var stages = new HFlowContainer();
        stages.AddChild(new Label { Text = "May work:" });
        foreach (var stage in StageOrder.All)
        {
            var box = new CheckBox { Text = stage.ToString()[..3] };
            _stageBoxes[stage] = box;
            stages.AddChild(box);
        }
        var applyStages = new Button { Text = "Apply stages" };
        applyStages.Pressed += () => TryApply(new SetAllowedStagesCommand(SelectedPerson().Id,
            _stageBoxes.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToHashSet()));
        stages.AddChild(applyStages);
        column.AddChild(stages);

        var promotion = new HBoxContainer();
        promotion.AddChild(new Label { Text = "Promote:" });
        _promotionOption = new OptionButton { CustomMinimumSize = new Vector2(120, 0) };
        promotion.AddChild(_promotionOption);
        var setPromotion = new Button { Text = "Set promotion" };
        setPromotion.Pressed += () =>
        {
            var id = _promotionOption.GetSelectedId();
            TryApply(new SetPromotionCommand(SelectedPerson().Id, id < 0 ? null : id));
        };
        promotion.AddChild(setPromotion);
        column.AddChild(promotion);

        column.AddChild(new Label { Text = "Queue (top = next):" });
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.4f };
        _queueBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_queueBox);
        column.AddChild(scroll);

        column.AddChild(new Label { Text = "Candidates (skills N/P/I/B/T, asking):" });
        var hiringScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.0f };
        _hiringBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hiringScroll.AddChild(_hiringBox);
        column.AddChild(hiringScroll);
        return column;
    }

    private Person SelectedPerson() =>
        (_personOption.ItemCount > 0 ? _state.FindPerson(_personOption.GetSelectedId()) : null) ?? _state.People[0];

    private Control BuildMarketColumn()
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.4f,
        };

        _rankingTitle = new Label { Text = "Market", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_rankingTitle);
        _rankingText = new RichTextLabel
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 2.2f,
            BbcodeEnabled = false,
            SelectionEnabled = true,
            FitContent = false,
        };
        column.AddChild(_rankingText);

        _studioPanelLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_studioPanelLabel);
        _premisesRow = new HFlowContainer();
        column.AddChild(_premisesRow);
        _amenityRow = new HFlowContainer();
        column.AddChild(_amenityRow);

        var money = new HBoxContainer();
        money.AddChild(new Label { Text = "Ledger:" });
        var online = new Button { Text = "Get Online" };
        online.Pressed += () => TryApply(new GetOnlineCommand());
        money.AddChild(online);
        column.AddChild(money);
        _ledgerText = new RichTextLabel
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.2f,
            BbcodeEnabled = false,
            SelectionEnabled = true,
        };
        column.AddChild(_ledgerText);

        column.AddChild(new Label { Text = "Volumes:" });
        _volumesText = new RichTextLabel
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
            BbcodeEnabled = false,
            SelectionEnabled = true,
        };
        column.AddChild(_volumesText);
        return column;
    }

    private Control BuildLogColumn()
    {
        _log = new RichTextLabel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
            ScrollFollowing = true,
            BbcodeEnabled = false,
            SelectionEnabled = true,
        };
        return _log;
    }

    // ---------------------------------------------------------------- refresh

    private void Refresh()
    {
        _dirty = false;
        _clockLabel.Text = $"{_state.Clock.Now:ddd yyyy-MM-dd HH:mm}";
        _speedLabel.Text = _speed <= 0 ? "Paused " : $"{_speed:0}x ";
        RefreshSeriesOption();
        RefreshSeriesStatus();
        RefreshChapterGrid();
        RefreshPersonOption();
        RefreshPersonPanel();
        RefreshHiringPanel();
        RefreshStudioPanel();
        RefreshMarketColumn();
        RefreshStatusLines();
    }

    private void RefreshPersonOption()
    {
        var previous = _personOption.ItemCount > 0 ? _personOption.GetSelectedId() : -1;
        _personOption.Clear();
        foreach (var person in _state.People)
            _personOption.AddItem($"{person.Name} ({person.Role})", person.Id);
        var index = _personOption.GetItemIndex(previous);
        _personOption.Selected = index >= 0 ? index : 0;

        var person2 = SelectedPerson();
        var promotionPrevious = _promotionOption.ItemCount > 0 ? _promotionOption.GetSelectedId() : -2;
        _promotionOption.Clear();
        _promotionOption.AddItem("off", -1);
        _promotionOption.AddItem("whole studio", PromotionRules.WholeStudio);
        foreach (var series in _state.Series.Where(s => s.Publishing == PublishingStatus.Unpublished && s.Status == SeriesStatus.Active))
            _promotionOption.AddItem(series.Title, series.Id);
        var wanted = promotionPrevious == -2 ? (person2.PromotionSeriesId ?? -1) : promotionPrevious;
        var promotionIndex = _promotionOption.GetItemIndex(wanted);
        _promotionOption.Selected = promotionIndex >= 0 ? promotionIndex : 0;
    }

    private void RefreshHiringPanel()
    {
        foreach (var child in _hiringBox.GetChildren()) child.QueueFree();
        foreach (var candidate in _state.Candidates)
        {
            var row = new HBoxContainer();
            var skills = string.Join("/", StageOrder.All.Select(st => candidate.Skill(st)));
            row.AddChild(new Label
            {
                Text = $"{candidate.Name} {skills} {candidate.AskingSalary:N0}" + (candidate.IsScheduled ? " *" : "") + (candidate.IsReturning ? " (back)" : ""),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            var offer = new SpinBox { MinValue = 0, MaxValue = 5_000_000, Step = 1000, Value = candidate.AskingSalary, CustomMinimumSize = new Vector2(100, 0) };
            row.AddChild(offer);
            var hire = new Button { Text = "Hire" };
            var id = candidate.Id;
            hire.Pressed += () => TryApply(new HireCommand(id, (int)offer.Value));
            row.AddChild(hire);
            _hiringBox.AddChild(row);
        }
        if (_state.Candidates.Count == 0) _hiringBox.AddChild(new Label { Text = $"Nobody yet; the pool refreshes at the first issue close of each month." });
    }

    private void RefreshStudioPanel()
    {
        var premises = _state.CurrentPremises;
        var payroll = _state.People.Where(p => p.Role == PersonRole.Assistant).Sum(p => (long)p.Salary);
        var charge = PremisesRules.Charge(premises, _state.OwnedAmenities, _state.HasInternet, _state.PriceIndexNow);
        _studioPanelLabel.Text = $"Studio: {premises.Name}, {_state.People.Count}/{premises.Capacity} desks, atmosphere {_state.Atmosphere:+0;-0;0}, " +
                                 $"monthly {charge.Total:N0} (rent {charge.Rent:N0}) + payroll {payroll:N0}";

        foreach (var child in _premisesRow.GetChildren()) child.QueueFree();
        _premisesRow.AddChild(new Label { Text = "Move:" });
        foreach (var option in _state.StaffData.Premises.Where(p => p.Id != premises.Id))
        {
            var button = new Button { Text = $"{option.Name} ({option.Capacity}, {option.MonthlyRent:N0})" };
            var id = option.Id;
            button.Pressed += () => TryApply(new MovePremisesCommand(id));
            _premisesRow.AddChild(button);
        }

        foreach (var child in _amenityRow.GetChildren()) child.QueueFree();
        _amenityRow.AddChild(new Label { Text = "Owned: " + (_state.Studio.Amenities.Count == 0 ? "nothing" : string.Join(", ", _state.Studio.Amenities)) + " | Buy:" });
        foreach (var amenity in _state.StaffData.Amenities.Where(a => !_state.Studio.Amenities.Contains(a.Id)))
        {
            var button = new Button { Text = $"{amenity.Name} {amenity.Cost:N0}" };
            var id = amenity.Id;
            button.Pressed += () => TryApply(new BuyAmenityCommand(id));
            _amenityRow.AddChild(button);
        }
    }

    private void RefreshSeriesOption()
    {
        var previous = _seriesOption.ItemCount > 0 ? _seriesOption.GetSelectedId() : -1;
        _seriesOption.Clear();
        foreach (var series in _state.Series)
            _seriesOption.AddItem($"{series.Title} ({series.Status}, {series.Publishing})", series.Id);
        if (_seriesOption.ItemCount == 0) return;
        var index = _seriesOption.GetItemIndex(previous);
        _seriesOption.Selected = index >= 0 ? index : 0;
    }

    private Series? SelectedSeries() =>
        _seriesOption.ItemCount == 0 ? null : _state.FindSeries(_seriesOption.GetSelectedId());

    private void WithSelectedSeries(Action<int> action)
    {
        if (_seriesOption.ItemCount == 0)
        {
            LogLine("Command rejected: no series selected.");
            return;
        }
        action(_seriesOption.GetSelectedId());
    }

    private void RefreshSeriesStatus()
    {
        if (SelectedSeries() is not { } series)
        {
            _seriesStatusLabel.Text = "No series yet.";
            return;
        }
        var parts = new List<string> { $"{series.Title}: {series.Publishing}" };
        if (series.Contract is { } contract)
        {
            var magazine = _state.Publishers.Require(contract.MagazineId);
            parts.Add($"{magazine.Name} at {contract.FeePerPage:N0} yen/page, {contract.ChaptersPublished} chapters under contract");
        }
        if (series.PendingOffer is { } offer)
            parts.Add($"offer from {_state.Publishers.Require(offer.MagazineId).Name}: {offer.FeePerPage:N0} yen/page, first issue {offer.FirstIssueClose:d MMM yyyy}");
        if (series.OpenChapter is { } open)
            parts.Add($"open ch.{open.Number} editor {open.Editor}" + (open.IsOneShot ? " (one-shot)" : ""));
        var last = series.Chapters.LastOrDefault(c => c.Quality is not null);
        if (last is not null) parts.Add($"last quality {last.Quality}");
        if (series.LastRank is { } rank && series.Contract is { } c2)
            parts.Add($"last rank #{rank} (line #{_state.Publishers.Require(c2.MagazineId).CancellationRank})");
        parts.Add($"fanbase {series.Fanbase:N0}, impact {series.CulturalImpact:0.0}" + (series.IsIconic ? ", ICONIC" : ""));
        if (series.Contract is { } c3)
        {
            var strikes = _state.LiveStrikes(series, _state.Publishers.Require(c3.MagazineId)).Count;
            parts.Add($"strikes {strikes}, below line {series.WeeksBelowLine}" +
                      (series.WarningIssuedAt is { } warned ? $", WARNING since {warned:d MMM}" : ""));
        }
        _seriesStatusLabel.Text = string.Join(" | ", parts);
    }

    private void RefreshChapterGrid()
    {
        foreach (var child in _chapterGrid.GetChildren()) child.QueueFree();

        foreach (var header in new[] { "Series", "Ch", "Name", "Pencils", "Inks", "Backgrounds", "Tones", "Due", "Status", "Late", "Q", "Rank" })
            _chapterGrid.AddChild(new Label { Text = header });

        foreach (var series in _state.Series)
        {
            foreach (var chapter in series.Chapters)
            {
                _chapterGrid.AddChild(new Label { Text = series.Title });
                _chapterGrid.AddChild(new Label { Text = chapter.IsOneShot ? $"{chapter.Number}*" : chapter.Number.ToString() });
                foreach (var stage in StageOrder.All)
                {
                    var work = chapter.StageWork(stage);
                    var assignee = work.AssignedTo is { } a ? _state.FindAnyPerson(a) : null;
                    var cell = new VBoxContainer();
                    var bar = new ProgressBar
                    {
                        MinValue = 0,
                        MaxValue = Math.Max(work.HoursRequired, 0.001),
                        Value = work.IsDone ? work.HoursRequired : work.HoursDone,
                        ShowPercentage = true,
                        CustomMinimumSize = new Vector2(80, 0),
                        TooltipText = $"{work.Status}: {work.HoursDone:0.0}/{work.HoursRequired:0.0}h, {work.OvertimeHours:0}h overtime, {work.Contribution:0.0} quality, {assignee?.Name ?? "unassigned"}",
                    };
                    cell.AddChild(bar);
                    cell.AddChild(new Label { Text = Initials(assignee) + (work.ManualAssignee is not null ? "!" : ""), HorizontalAlignment = HorizontalAlignment.Center });
                    _chapterGrid.AddChild(cell);
                }
                _chapterGrid.AddChild(new Label { Text = chapter.DueDate.ToString("MM-dd HH:mm") });
                var status = chapter.Status.ToString();
                if (chapter.IsAtRisk && chapter.Status != ChapterStatus.Complete) status += " (at risk)";
                if (chapter.Editor != EditorStatus.NotRequired) status += $" [{chapter.Editor}]";
                if (chapter.IsPublished) status += " published";
                _chapterGrid.AddChild(new Label { Text = status });
                _chapterGrid.AddChild(new Label { Text = chapter.IsLate ? $"late {chapter.HoursOverdue}h" : "" });
                _chapterGrid.AddChild(new Label { Text = chapter.Quality?.ToString() ?? "" });
                _chapterGrid.AddChild(new Label { Text = chapter.Rank is { } r ? $"#{r}" : "" });
            }
        }
    }

    private void RefreshPersonPanel()
    {
        var person = SelectedPerson();
        var skills = string.Join("/", StageOrder.All.Select(st => person.Skill(st)));
        _personLabel.Text = $"{person.Name} (rep {person.Reputation:0.0}, skills {skills}, {person.Salary:N0} yen)  today: {person.HoursWorkedToday}h (+{person.OvertimeHoursToday} OT, {person.BreaksToday} breaks)  " +
                            $"current: {(person.OnBreak ? "on a break" : person.CurrentTask is { } task ? Describe(task.ChapterId, task.Stage) : person.PromotionSeriesId is not null ? "promoting" : "idle")}";
        _moodLabel.Text = $"happiness {person.Happiness:0} (equilibrium {_state.EquilibriumOf(person):0}), fatigue {person.Fatigue:0}, " +
                          $"hunger {person.Needs.Hunger:0} thirst {person.Needs.Thirst:0} comfort {person.Needs.Comfort:0}" +
                          (person.IsMoonlighting ? ", MOONLIGHTING" : "") +
                          (person.Role == PersonRole.Assistant ? $", {person.MonthsEmployed} months" : "");

        foreach (var child in _queueBox.GetChildren()) child.QueueFree();
        var queue = person.Queue;
        for (var i = 0; i < queue.Count; i++)
        {
            var reference = queue[i];
            var index = i;
            var row = new HBoxContainer();
            var pinned = person.Pins.Contains(reference);
            row.AddChild(new Label
            {
                Text = (pinned ? "* " : "") + Describe(reference.ChapterId, reference.Stage),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });

            var pin = new Button { Text = pinned ? "Unpin" : "Pin" };
            pin.Pressed += () => TryApply(pinned
                ? new UnpinStageCommand(person.Id, reference.ChapterId, reference.Stage)
                : new PinStageCommand(person.Id, reference.ChapterId, reference.Stage));
            row.AddChild(pin);

            var up = new Button { Text = "Up", Disabled = index == 0 };
            up.Pressed += () => Reorder(person, index, index - 1);
            row.AddChild(up);

            var down = new Button { Text = "Down", Disabled = index == queue.Count - 1 };
            down.Pressed += () => Reorder(person, index, index + 1);
            row.AddChild(down);

            var skip = new Button { Text = "Skip" };
            skip.Pressed += () => TryApply(new SkipStageCommand(reference.ChapterId, reference.Stage));
            row.AddChild(skip);

            _queueBox.AddChild(row);
        }
    }

    private void RefreshMarketColumn()
    {
        var magazine = SelectedMagazine();
        var market = _state.MarketOf(magazine.Id);
        _rankingTitle.Text = $"{magazine.Name}: issue {market.IssuesClosed}, next close {market.NextIssueClose:ddd d MMM HH:mm}, line #{magazine.CancellationRank}";
        _rankingText.Clear();
        if (market.LastRanking.Count == 0)
        {
            _rankingText.AddText("No issue closed yet.\n");
        }
        foreach (var entry in market.LastRanking)
        {
            var mine = entry.SeriesId is not null;
            _rankingText.AddText($"{(mine ? ">>" : "  ")} #{entry.Rank,2} {entry.Title} ({entry.Score:0.0})\n");
        }

        _ledgerText.Clear();
        _ledgerText.AddText($"Balance {_state.Money:N0} yen | price index {_state.PriceIndexNow:0.00} | " +
                            (_state.HasInternet ? "online" : $"offline (getting online costs {_state.InternetCostNow:N0})") + "\n");
        foreach (var entry in _state.Ledger.TakeLast(LedgerTailLines))
            _ledgerText.AddText($"{entry.Time:MM-dd} {entry.Amount,12:N0} {entry.Reason}\n");

        _volumesText.Clear();
        foreach (var series in _state.Series)
        {
            foreach (var volume in series.Volumes)
            {
                var status = !volume.IsReleased ? $"out {volume.ReleaseDate:d MMM yyyy}" : $"{volume.CopiesSold:N0} sold, {volume.WeeksOnSale} wk";
                _volumesText.AddText($"{series.Title} vol.{volume.Number} ch.{volume.FirstChapter}-{volume.LastChapter}{(volume.IsDoujin ? " doujin" : "")}: {status}\n");
            }
        }
    }

    private void RefreshStatusLines()
    {
        _studioLabel.Text = $"Studio: track record {_state.StudioTrackRecord:0.0}, staff {_state.StaffTerm:0.0}, effective {_state.EffectiveReputation:0.0}";
        var trends = _state.Trends.Select(t =>
        {
            var effective = TrendRules.Effective(t, _state.TrendData, _state.Clock.Now);
            var influence = t.PlayerInfluence > 0 ? $"+{t.PlayerInfluence:0.00}" : "";
            var boom = t.BoomEndsAt is not null ? "!" : "";
            return $"{t.Genre} {effective:0.00}{influence}{boom}";
        });
        _trendsLabel.Text = "Trends: " + string.Join("  ", trends);
    }

    private void Reorder(Person person, int from, int to)
    {
        var order = person.Queue.ToList();
        if (to < 0 || to >= order.Count) return;
        (order[from], order[to]) = (order[to], order[from]);
        TryApply(new ReorderQueueCommand(person.Id, order));
    }

    /// <summary>Copies the person's schedule and overtime flag into the input widgets. Called on start and after Load only, so edits in progress are not clobbered.</summary>
    private void ResetPersonInputs()
    {
        var person = SelectedPerson();
        _startSpin.Value = person.Schedule.WorkStartHour;
        _endSpin.Value = person.Schedule.WorkEndHour;
        foreach (var (day, box) in _dayOffBoxes) box.ButtonPressed = person.Schedule.DaysOff.Contains(day);
        _overtimeBox.SetPressedNoSignal(person.OvertimeAllowed);
        _salarySpin.Value = person.Salary;
        foreach (var (stage, box) in _stageBoxes) box.ButtonPressed = person.AllowedStages.Contains(stage);
    }

    // ---------------------------------------------------------------- helpers

    private string Describe(int chapterId, Stage? stage = null)
    {
        var chapter = _state.FindChapter(chapterId);
        if (chapter == null) return $"chapter #{chapterId}";
        var series = _state.SeriesOf(chapter);
        var text = $"{series.Title} ch.{chapter.Number}";
        return stage is { } s ? $"{text} {s}" : text;
    }

    private string DescribeChapter(int seriesId, int chapterNumber, Stage? stage = null)
    {
        var series = _state.FindSeries(seriesId);
        var title = series?.Title ?? $"series #{seriesId}";
        var text = $"{title} ch.{chapterNumber}";
        return stage is { } s ? $"{text} {s}" : text;
    }

    private static string Initials(Person? person) =>
        person is null ? "-" : string.Concat(person.Name.Split(' ').Where(w => w.Length > 0).Select(w => w[0]));

    private void AppendLog(GameEvent ev) => _log.AddText($"[{ev.Time:ddd MM-dd HH:mm}] {ev.Type}: {ev.Message}\n");

    private void LogLine(string text) => _log.AddText($"[{_state.Clock.Now:ddd MM-dd HH:mm}] {text}\n");
}
