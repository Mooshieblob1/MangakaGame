using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Work feedback (approved 2026-10-03): stage-coloured sparkles flow from each person working on the header series
    // into that stage of the header progress bar while the office is the main view, and a bubble says when a stage
    // finishes. WorkFeedbackPlan paces both; reduced interface motion turns both off.
    private readonly WorkFeedbackPlan _workFeedback = new();
    private WorkSparkleLayer _sparkleLayer = null!;
    private GameState? _workFeedbackState;
    internal int WorkBubblesShown { get; private set; }
    internal int SoldPulses { get; private set; }
    internal int FirstSaleBubbles { get; private set; }
    private HashSet<int>? _onSale;
    private bool? _hadSale;
    private int? _salesBusiness;
    private (DateTime Now, int Revision)? _salesKey;
    internal int SalesRecomputes { get; private set; }

    private void BuildWorkFeedback()
    {
        _sparkleLayer = new WorkSparkleLayer { Name = "WorkSparkles", MouseFilter = MouseFilterEnum.Ignore, ZIndex = 20 };
        AddChild(_sparkleLayer); _sparkleLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    private bool OfficeIsMainView => _managementReady && !TitleOpen && !_inMenu && !_side.Visible && !_report.Visible &&
        !_helperPopup.Visible && !_storyOpen && !OfficeEditing && _homeOffice.IsVisibleInTree();

    private void UpdateWorkFeedback(double delta)
    {
        if (_sparkleLayer is null) return;
        _sparkleLayer.Advance(delta);
        if (!_managementReady || TitleOpen) { _workFeedback.Reset(); _workFeedbackState = null; ResetSalesWatch(); return; }
        // Loading a career or rewinding starts from what is already drawn; it never announces old work.
        if (!ReferenceEquals(_workFeedbackState, _state)) { _workFeedback.Reset(); _workFeedbackState = _state; ResetSalesWatch(); }
        ObserveSales();
        var series = _state.Series.FirstOrDefault(s => s.Id == _progressSeriesId);
        var chapter = _currentStageBar.Chapter;
        if (series is null || chapter is null) { _workFeedback.Sparkles(delta, 0, []); return; }
        _workFeedback.Observe(series.Id, series.Chapters.SelectMany(c => c.Stages.Select(s => (c.Id, s.Stage, s.IsDone))));
        var motion = !_presentation.ReducedUiMotion;
        if (_workFeedback.NextBubble(delta) is { } finished && motion && !_inMenu && _currentStageBar.IsVisibleInTree()) ShowStageBubble(finished);

        var speed = OfficeIsMainView && motion && _overnightTarget is null ? _speed : 0;
        var working = SparklingAtDesks(chapter);
        foreach (var personId in _workFeedback.Sparkles(delta, speed, working.Keys))
        {
            var stage = working[personId];
            if (_homeOffice.PersonScreenPoint(personId) is not { } from || _currentStageBar.StagePoint(stage) is not { } to) continue;
            _sparkleLayer.Launch(from, to, StageColour(stage));
        }
    }

    // People drawing the header chapter right now, by the stage they are drawing. The simulation counts whole work
    // hours; the office adds toilet and break-room trips, so sparkles come only from people seated at their desks.
    private Dictionary<int, Stage> SparklingAtDesks(Chapter chapter) =>
        WorkingOn(chapter).Where(w => _homeOffice.AtDeskWorking(w.Key)).ToDictionary(w => w.Key, w => w.Value);

    private Dictionary<int, Stage> WorkingOn(Chapter chapter) =>
        _state.OfficeActivities.GroupBy(a => a.PersonId).Select(g => g.Last())
            .Where(a => a.Kind == OfficeActivityKind.Work && a.Stage is { } stage && !chapter.StageWork(stage).IsDone &&
                _state.People.FirstOrDefault(p => p.Id == a.PersonId)?.CurrentTask is { } task && task.ChapterId == chapter.Id && task.Stage == stage)
            .ToDictionary(a => a.PersonId, a => a.Stage!.Value);

    private static Color StageColour(Stage stage) => ChapterProgressBar.Stages.First(s => s.Stage == stage).Color;
    private static string StageLabel(Stage stage) => ChapterProgressBar.Stages.First(s => s.Stage == stage).Name;

    private void ShowStageBubble(Stage stage)
    {
        if (_currentStageBar.StagePoint(stage) is not { } point) return;
        WorkBubblesShown++;
        ShowBubble(new(point.X, point.Y + _currentStageBar.Size.Y / 2), StageLabel(stage) + " done!", StageColour(stage), 6);
    }

    // A small coloured bubble that drops in below a point, waits and fades. Shared by stage and first-sale bubbles.
    private void ShowBubble(Vector2 point, string text, Color colour, float below)
    {
        var bubble = new Label { Name = "StageBubble", Text = text, MouseFilter = MouseFilterEnum.Ignore, ZIndex = 21 };
        var box = new StyleBoxFlat { BgColor = colour }; box.SetCornerRadiusAll(10); box.SetContentMarginAll(6);
        box.ContentMarginLeft = box.ContentMarginRight = 12; box.BorderColor = new(BrandPalette.Ink); box.SetBorderWidthAll(2);
        bubble.AddThemeStyleboxOverride("normal", box);
        bubble.AddThemeColorOverride("font_color", new(BrandPalette.Ink));
        bubble.AddThemeFontSizeOverride("font_size", (int)(15 * _presentation.UiScale));
        AddChild(bubble);
        var size = bubble.GetCombinedMinimumSize();
        var view = GetViewportRect().Size;
        var start = new Vector2(Math.Clamp(point.X - size.X / 2, 8, Math.Max(8, view.X - size.X - 8)), point.Y + below);
        bubble.Position = start;
        var tween = bubble.CreateTween();
        tween.TweenProperty(bubble, "position", start + new Vector2(0, 14), .35).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(1.1);
        tween.TweenProperty(bubble, "modulate:a", 0f, .5);
        tween.TweenCallback(Callable.From(bubble.QueueFree));
    }

    // The selling tutorial (spec 2026-10-03): the Sold count pulses when a book's first copies go on sale, and the
    // career's first copy sold gets a bubble. Loading a career is the starting point, never a new event.
    // It runs every frame, so it looks again only when the simulation moved (the clock, or a command's office revision).
    private void ObserveSales()
    {
        // A move into another business is a first look too; once the career had a sale, it never gets the bubble again.
        if (_salesBusiness != _state.ControlledBusinessId)
        { _salesBusiness = _state.ControlledBusinessId; _onSale = null; _salesKey = null; if (_hadSale != true) _hadSale = null; }
        var key = (_state.Clock.Now, _state.OfficeRevision);
        if (_salesKey == key) return;
        _salesKey = key; SalesRecomputes++;
        var mine = _state.Series.Where(s => s.BusinessId == _state.ControlledBusinessId).SelectMany(s => s.Volumes)
            .Where(v => v.ReleasedAt is not null && (_state.Stock(v.Id) > 0 || _state.DoujinOnlineListed(v.Id) || !v.IsDoujin)).Select(v => v.Id).ToHashSet();
        var sale = _hadSale == true || _state.Series.Where(s => s.BusinessId == _state.ControlledBusinessId).Any(s => _state.SeriesCopiesSold(s.Id) > 0);
        if (_onSale is not null && mine.Except(_onSale).Any()) PulseSold();
        if (_hadSale == false && sale) FirstSale();
        _onSale = mine; _hadSale = sale;
    }

    private void ResetSalesWatch() { _onSale = null; _hadSale = null; _salesKey = null; _salesBusiness = null; }

    private void PulseSold()
    {
        SoldPulses++;
        var label = _currentCopies; var gold = new Color(BrandPalette.Gold);
        if (_presentation.ReducedUiMotion)
        { label.Modulate = gold; GetTree().CreateTimer(3).Timeout += () => { if (IsInstanceValid(label)) label.Modulate = Colors.White; }; return; }
        var tween = label.CreateTween().SetLoops(3);
        tween.TweenProperty(label, "modulate", gold, .25); tween.TweenProperty(label, "modulate", Colors.White, .25);
    }

    private void FirstSale()
    {
        FirstSaleBubbles++;
        if (_presentation.ReducedUiMotion || !_currentCopies.IsVisibleInTree()) { Notify("Helper-Chan: First copy sold!"); return; }
        var rect = _currentCopies.GetGlobalRect();
        ShowBubble(new(rect.GetCenter().X, rect.End.Y), "First copy sold!", new(BrandPalette.Gold), 6);
    }
}

/// <summary>Draws the work sparkles: small glowing dots arcing from a worker's head to the progress bar.</summary>
public partial class WorkSparkleLayer : Control
{
    private const double Seconds = 1.0;
    private static readonly Color Outline = new(BrandPalette.Ink);
    private readonly List<(Vector2 From, Vector2 Bend, Vector2 To, Color Colour, double Age, double Life)> _sparkles = new();
    private readonly Random _random = new();
    public int Count => _sparkles.Count;
    public int Launched { get; private set; }

    public void Launch(Vector2 from, Vector2 to, Color colour)
    {
        var local = GetGlobalTransformWithCanvas().AffineInverse();
        var a = local * from; var b = local * to;
        // The arc bows sideways a little at random so several workers' sparkles do not share one line.
        var bend = (a + b) / 2 + new Vector2((float)(_random.NextDouble() - .5) * 160, -Math.Abs(b.Y - a.Y) * .25f - 40);
        _sparkles.Add((a, bend, b, colour, 0, Seconds * (.85 + _random.NextDouble() * .3)));
        Launched++;
        QueueRedraw();
    }

    public void Advance(double delta)
    {
        if (_sparkles.Count == 0) return;
        for (var i = 0; i < _sparkles.Count; i++) _sparkles[i] = _sparkles[i] with { Age = _sparkles[i].Age + delta };
        _sparkles.RemoveAll(s => s.Age >= s.Life);
        QueueRedraw();
    }

    public void Clear() { _sparkles.Clear(); QueueRedraw(); }

    public override void _Draw()
    {
        foreach (var s in _sparkles)
        {
            var t = (float)(s.Age / s.Life);
            var fade = t < .15f ? t / .15f : t > .85f ? (1 - t) / .15f : 1;
            // A short comet tail of fading dots behind the head.
            for (var trail = 4; trail >= 1; trail--)
            {
                var at = PointAt(s, Math.Max(0, t - trail * .035f)); var radius = 6f - trail * .9f; var alpha = (.5f - trail * .1f) * fade;
                DrawCircle(at, radius + 1.5f, new Color(Outline, alpha)); DrawCircle(at, radius, new Color(s.Colour, alpha));
            }
            var point = PointAt(s, t);
            // Slightly larger, with a dark outline, so they read against both light floors and dark panels.
            DrawCircle(point, 13, new Color(s.Colour, .22f * fade));
            DrawCircle(point, 8.5f, new Color(Outline, .85f * fade));
            DrawCircle(point, 6.5f, new Color(s.Colour, fade));
            DrawCircle(point, 2.5f, new Color(1, 1, 1, .9f * fade));
        }
    }

    private static Vector2 PointAt((Vector2 From, Vector2 Bend, Vector2 To, Color Colour, double Age, double Life) s, float t)
    {
        var eased = 1 - (1 - t) * (1 - t); // quick start, gentle arrival
        return (1 - eased) * (1 - eased) * s.From + 2 * (1 - eased) * eased * s.Bend + eased * eased * s.To;
    }
}
