using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>
/// Debug harness for the simulation core: clock and speed controls, a chapter table,
/// a person panel with queue editing, an event log, and save/load. All UI is built in code.
/// </summary>
public partial class DebugMain : Control
{
    // Ten-hour default workday: 10 * 36 / 8 = 45 real seconds at 8x.
    // This stretches the clock only; actor movement still receives the chosen speed.
    private const double SecondsPerHourAt1x = 36;
    private string _savePath = "user://debug-v6.json";
    private const int LogLinesOnLoad = 200;

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
    private OptionButton _genreOption = null!;
    private OptionButton _cadenceOption = null!;
    private SpinBox _pagesSpin = null!;
    private OptionButton _seriesOption = null!;
    private OptionButton _setCadenceOption = null!;
    private SpinBox _setPagesSpin = null!;

    private GridContainer _chapterGrid = null!;

    private Label _personLabel = null!;
    private SpinBox _startSpin = null!;
    private SpinBox _endSpin = null!;
    private readonly Dictionary<DayOfWeek, CheckBox> _dayOffBoxes = new();
    private CheckBox _overtimeBox = null!;
    private VBoxContainer _queueBox = null!;

    public override void _Ready()
    {
        if(OS.GetCmdlineUserArgs().Contains("--smoke-test")) ManagementInterface=false;
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride($"margin_{edge}", 12);
        AddChild(margin);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        root.AddChild(BuildTimelineControls());
        _mainTabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(_mainTabs);
        var production = new HBoxContainer { Name = "Production" };
        production.AddThemeConstantOverride("separation", 12);
        production.AddChild(BuildLeftColumn());
        production.AddChild(BuildPersonColumn());
        production.AddChild(BuildLogColumn());
        _mainTabs.AddChild(production);
        _mainTabs.AddChild(BuildPublishingPanel());
        _mainTabs.AddChild(BuildStaffPanel());
        _mainTabs.AddChild(BuildOperationsPanel());
        _mainTabs.AddChild(BuildTokyoPanel());
        _mainTabs.AddChild(BuildOfficePanel());
        _mainTabs.AddChild(BuildIndustryPanel());

        _recapDialog = new AcceptDialog { Title = "Daily recap", OkButtonText = "Continue", Exclusive = true };
        _recapDialog.Confirmed += OnRecapContinue;
        AddChild(_recapDialog);

        _scanIndex = 0;
        foreach (var ev in _state.Events) AppendLog(ev);
        _scanIndex = _state.Events.Count;
        ResetPersonInputs();
        Refresh();
        if (ManagementInterface) BuildManagementShell(margin);
        if (OS.GetCmdlineUserArgs().Contains("--smoke-test")) CallDeferred(nameof(RunSmokeTest));
        if (OS.GetCmdlineUserArgs().Contains("--management-smoke")) CallDeferred(nameof(RunManagementSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--progression-smoke")) CallDeferred(nameof(RunProgressionSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--alpha-smoke")) CallDeferred(nameof(RunAlphaSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--usability-smoke")) CallDeferred(nameof(RunUsabilitySmoke));
        if (OS.GetCmdlineUserArgs().Contains("--production-smoke")) CallDeferred(nameof(RunProductionSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--office-life-smoke")) CallDeferred(nameof(RunOfficeLifeSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--convenience-smoke")) CallDeferred(nameof(RunConvenienceSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--series-status-smoke")) CallDeferred(nameof(RunSeriesStatusSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--atmosphere-smoke")) CallDeferred(nameof(RunAtmosphereSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--family-home-smoke")) CallDeferred(nameof(RunFamilyHomeSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--quiet-speed-smoke")) CallDeferred(nameof(RunQuietSpeedSmoke));
        if (OS.GetCmdlineUserArgs().Contains("--display-sweep-smoke")) CallDeferred(nameof(RunDisplaySweepSmoke));
    }

    public override void _Process(double delta)
    {
        if(_managementReady)PanWithKeys(delta);
        if(_managementReady)_audio.Update(delta,!_inMenu&&GetWindow().HasFocus(),_speed>0&&!OfficeEditing,_presentation.AmbienceVolume,_presentation.EffectsVolume);
        if (_overnightTarget is not null) TickOvernight(delta);
        else if (_speed > 0)
        {
            _accumulator += delta * _speed / SecondsPerHourAt1x;
            var whole = (int)Math.Floor(_accumulator);
            if (whole > 0)
            {
                _accumulator -= whole;
                AdvanceAndScan(whole);
            }
        }

        if(_managementReady)TickMoneyFeedback(delta);
        if (_dirty) Refresh();
        var visualSpeed=OfficePlaybackSpeed;
        if (_managementReady){_homeOffice.Speed=visualSpeed;_homeOffice.HourFraction=_accumulator;}
        if (_officeView is not null){_officeView.Speed = OfficeEditing ? 0 : visualSpeed;_officeView.HourFraction=_accumulator;}
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

        // At 32x routine days roll on; only events that need the player stop the game (Q27).
        var fast = _speed >= QuietSpeed && _overnightTarget is null && _managementReady;
        var prefs = _presentation.Guidance;
        var threadBefore = prefs.Thread.Count;
        GameEvent? recap = null;
        var shouldPause = false;
        var stop = false;
        foreach (var ev in fresh)
        {
            AppendLog(ev);
            // A missed payday becomes Helper-Chan's text; the pop-up remains only when her guidance is hidden.
            var texted = ev.Type == EventType.WageArrears && CareerGuidance.ReportArrears(_state, prefs, ev) && prefs.Visible;
            if (_managementReady && !texted) QueueImportantEvent(ev,_state.Events.IndexOf(ev));
            if (ev.Type == EventType.DailyRecap) recap = ev;
            if (fast && (CareerGuidance.FastSpeedStops.Contains(ev.Type) || texted)) { shouldPause = true; stop = true; }
            else if (fast && ev.Type is EventType.DailyRecap or EventType.ChapterCompleted) { }
            else if (ev.Type == EventType.DailyRecap ||
                (_state.Settings.AutoPause.TryGetValue(ev.Type, out var pause) && pause))
                shouldPause = true;
        }
        if (fast && prefs.Visible)
        {
            CareerGuidance.Observe(_state, prefs);
            if (prefs.Thread.Count > threadBefore && CareerGuidance.Unread(prefs) > 0) { shouldPause = true; stop = true; }
        }
        // Helper-Chan introduces 32x once, after a working day with nothing that needed the player (Q28).
        if (!fast && recap != null && _managementReady && prefs.Visible && _overnightTarget is null &&
            CareerGuidance.OfferQuietSpeed(_state, prefs, recap.Time.AddHours(-24))) _dirty = true;

        // Overnight bookkeeping still runs, but notices wait for the following morning.
        if(_overnightTarget is not null)return false;
        if (stop) { Pause(); _resumeSpeed = _daySpeed; }
        else if (shouldPause) Pause();
        if (fast && recap != null && !shouldPause) { BeginOvernight(); return true; }
        if (recap != null) ShowRecap(recap);
        return shouldPause;
    }

    private void SetSpeed(double speed)
    {
        if(_overnightTarget is not null)
        {
            // Pause the presentation without discarding its destination or daytime speed.
            var playback=speed>0?OvernightSpeed:0;
            var playbackChanged=_speed!=playback;_speed=playback;
            RefreshOvernightCaption();RefreshSpeedFeedback(playbackChanged);_dirty=true;return;
        }
        CancelOvernight();
        var changed=_speed!=speed;
        if (speed > 0) _resumeSpeed = speed;
        if (speed > 0 && speed < QuietSpeed) _daySpeed = speed;
        _speed = speed;
        RefreshSpeedFeedback(changed);
        _accumulator = 0;
        _dirty = true;
    }

    private void Pause() => SetSpeed(0);
    private void TogglePause() => SetSpeed(_speed>0?0:_resumeSpeed);

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
            lines.Add($"Income: ¥{recap.YenEarned:N0}");
            foreach (var c in recap.ChaptersPublished) lines.Add($"Published: {DescribeChapter(c.SeriesId, c.ChapterNumber)}");
            foreach (var id in recap.IssuesMissed) lines.Add($"Issue missed: {_state.FindSeries(id)?.Title}");
        }
        _recapDialog.DialogText = string.Join("\n", lines);
        // The headless display has no native desktop rectangle to centre within.
        if (DisplayServer.GetName() == "headless") _recapDialog.Popup(new Rect2I(20, 20, 800, 450));
        else _recapDialog.PopupCentered();
    }

    private void OnRecapContinue()
    {
        _recapDialog.Hide();
        if(_managementReady)BeginOvernight();
        else if (!AdvanceAndScan(_state.HoursUntilNextWork())) SetSpeed(_resumeSpeed);
    }

    private void TryApply(ICommand command)
    {
        try
        {
            _state.Apply(command);
            _publishingFeedback.Text = "";
            _dirty = true;
            ScanEvents();
        }
        catch (InvalidCommandException ex)
        {
            LogLine($"Command rejected: {ex.Message}");
            if(_managementReady)Notify(ex.Message);
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
        SaveOfficePreferences();
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

        CancelOvernight();_state = loaded;
        LoadOfficePreferences();
        _recapDialog.Hide();
        Pause();
        _log.Clear();
        foreach (var ev in _state.Events.TakeLast(LogLinesOnLoad)) AppendLog(ev);
        _scanIndex = _state.Events.Count;
        LogLine("Loaded.");
        ResetPersonInputs();
        _dirty = true;
        var recap = _state.Events.LastOrDefault(e => e.Type == EventType.DailyRecap);
        if (_state.RecapFiredToday && recap?.Time == _state.Clock.Now) ShowRecap(recap);
    }

    // ---------------------------------------------------------------- UI construction

    private Control BuildTimelineControls()
    {
        var column = new HFlowContainer();

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
        return column;
    }

    private Control BuildLeftColumn()
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 2.8f,
        };

        column.AddChild(new Label{Text="Ongoing series: one chapter = one printable issue; five chapters = a collected book. Cadence is a personal target until you accept a publisher contract.",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        column.AddChild(BuildSeriesForm());
        column.AddChild(BuildSeriesControls());

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _chapterGrid = new GridContainer { Columns = 10, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_chapterGrid);
        column.AddChild(scroll);
        return column;
    }

    private Control BuildSeriesForm()
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "New ongoing series:" });
        _titleEdit = new LineEdit { PlaceholderText = "Title", CustomMinimumSize = new Vector2(140, 0) };
        row.AddChild(_titleEdit);
        _genreOption = MakeGenreOption();
        row.AddChild(_genreOption);
        _cadenceOption = MakeCadenceOption();
        row.AddChild(_cadenceOption);
        _pagesSpin = new SpinBox { MinValue = 1, MaxValue = 200, Value = 19, Step = 1 };
        row.AddChild(_pagesSpin);
        var create = new Button { Text = "Create" };
        create.Pressed += () => TryApply(new CreateSeriesCommand(
            _titleEdit.Text, _genreOption.GetItemText(_genreOption.Selected), (Cadence)_cadenceOption.GetSelectedId(), (int)_pagesSpin.Value,true));
        row.AddChild(create);
        return row;
    }

    private Control BuildSeriesControls()
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "Series:" });
        _seriesOption = new OptionButton { CustomMinimumSize = new Vector2(140, 0) };
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
            SizeFlagsStretchRatio = 1.3f,
        };

        _personOption = new OptionButton();
        _personOption.ItemSelected += _ => { _selectedPersonId = _personOption.GetSelectedId(); ResetPersonInputs(); _dirty = true; };
        column.AddChild(_personOption);
        _personLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        column.AddChild(_personLabel);

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
            var person = SelectedPerson;
            var daysOff = _dayOffBoxes.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToHashSet();
            TryApply(new SetScheduleCommand(person.Id, (int)_startSpin.Value, (int)_endSpin.Value, daysOff));
        };
        column.AddChild(apply);

        _overtimeBox = new CheckBox { Text = "Overtime allowed" };
        _overtimeBox.Toggled += on => TryApply(new SetOvertimeAllowedCommand(SelectedPerson.Id, on));
        column.AddChild(_overtimeBox);

        column.AddChild(new Label { Text = "Queue (top = next):" });
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _queueBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_queueBox);
        column.AddChild(scroll);
        return column;
    }

    private Control BuildLogColumn()
    {
        _log = new RichTextLabel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.1f,
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
        RefreshChapterGrid();
        RefreshPersonPanel();
        RefreshPublishing();
        RefreshStaff();
        RefreshOperations();
        RefreshOffice();
        RefreshTokyo();
        RefreshIndustry();
        if (_managementReady) RefreshManagement();
    }

    private void RefreshSeriesOption()
    {
        var previous = _seriesOption.ItemCount > 0 ? _seriesOption.GetSelectedId() : -1;
        SyncOptions(_seriesOption,_state.Series.Where(s=>!ManagementInterface||s.BusinessId==_state.ControlledBusinessId&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).Select(s=>(s.Id,$"{s.Title} ({s.Status})")),previous);
    }

    private void WithSelectedSeries(Action<int> action)
    {
        if (_seriesOption.ItemCount == 0)
        {
            LogLine("Command rejected: no series selected.");
            return;
        }
        action(_seriesOption.GetSelectedId());
    }

    private void RefreshChapterGrid()
    {
        foreach (var child in _chapterGrid.GetChildren()) child.QueueFree();

        foreach (var header in new[] { "Series", "Ch", "Name", "Pencils", "Inks", "Backgrounds", "Tones", "Target / deadline", "Status", "Late" })
            _chapterGrid.AddChild(new Label { Text = header });

        foreach (var series in _state.Series)
        {
            foreach (var chapter in series.Chapters)
            {
                _chapterGrid.AddChild(new Label { Text = series.Title });
                _chapterGrid.AddChild(new Label { Text = chapter.Number.ToString() });
                foreach (var stage in StageOrder.All)
                {
                    var work = chapter.StageWork(stage);
                    var bar = new ProgressBar
                    {
                        MinValue = 0,
                        MaxValue = Math.Max(work.HoursRequired, 0.001),
                        Value = work.IsDone ? work.HoursRequired : work.HoursDone,
                        ShowPercentage = true,
                        CustomMinimumSize = new Vector2(90, 0),
                        TooltipText = $"{work.Status}: {work.HoursDone:0.0}/{work.HoursRequired:0.0}h",
                    };
                    _chapterGrid.AddChild(bar);
                }
                _chapterGrid.AddChild(new Label { Text = (_state.HasPublisherDeadline(chapter)?"Deadline ":"Target ")+chapter.DueDate.ToString("MM-dd HH:mm") });
                var status = chapter.Status.ToString();
                if (chapter.IsAtRisk && chapter.Status != ChapterStatus.Complete) status += " (at risk)";
                _chapterGrid.AddChild(new Label { Text = status });
                _chapterGrid.AddChild(new Label { Text = chapter.IsLate ? $"late {chapter.HoursOverdue}h" : "" });
            }
        }
    }

    private void RefreshPersonPanel()
    {
        var person = SelectedPerson;
        SyncOptions(_personOption,_state.ControlledStaff.Select(p=>(p.Id,p.Name)),person.Id);
        _personLabel.Text = $"{person.Name}  today: {person.HoursWorkedToday}h (+{person.OvertimeHoursToday} OT)  " +
                            $"current: {(person.CurrentTask is { } task ? Describe(task.ChapterId, task.Stage) : "idle")}";

        var queue = person.Queue;
        var queueKey=person.Id+":"+string.Join(";",queue.Select(q=>$"{q.ChapterId}:{q.Stage}:{person.Pins.Contains(q)}"));
        if(_queueBox.HasMeta("queue")&&_queueBox.GetMeta("queue").AsString()==queueKey)return;
        _queueBox.SetMeta("queue",queueKey);Empty(_queueBox);
        for (var i = 0; i < queue.Count; i++)
        {
            var reference = queue[i];
            var index = i;
            var row = new HFlowContainer();
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
        var person = SelectedPerson;
        _startSpin.Value = person.Schedule.WorkStartHour;
        _endSpin.Value = person.Schedule.WorkEndHour;
        foreach (var (day, box) in _dayOffBoxes) box.ButtonPressed = person.Schedule.DaysOff.Contains(day);
        _overtimeBox.SetPressedNoSignal(person.OvertimeAllowed);
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

    private void AppendLog(GameEvent ev) => _log.AddText($"[{ev.Time:ddd MM-dd HH:mm}] {ev.Type}: {ev.Message}\n");

    private void LogLine(string text)
    {
        _log.AddText($"[{_state.Clock.Now:ddd MM-dd HH:mm}] {text}\n");
        _publishingFeedback.Text = text;
    }
}
