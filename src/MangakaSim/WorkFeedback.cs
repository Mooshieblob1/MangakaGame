namespace MangakaSim;

/// <summary>
/// Paces the work feedback (2026-10-03): sparkles flowing from each worker into the progress bar, and a bubble when a
/// stage finishes. Presentation only: it reads what the simulation already shows and never changes play or replays.
/// </summary>
public sealed class WorkFeedbackPlan
{
    public const double SparklesPerSecondAt8x = 3, SpeedCap = 32, BubbleSpacing = 1, BubbleLifetime = 3;
    private const int MaxBurst = 3, MaxQueued = 3;
    private readonly Dictionary<int, double> _due = new();
    private readonly List<(Stage Stage, double Age)> _bubbles = new();
    private HashSet<int>? _chapters;
    private HashSet<(int Chapter, Stage Stage)> _done = new();
    private int _series;
    private double _sinceBubble = BubbleSpacing;

    /// <summary>
    /// The share of work time a worker sits at the desk (2026-10-03): the office shows a break-room visit about once a
    /// game hour (about 5 of every 38 one-speed seconds) and a toilet trip every 3 to 4 hours (about 20 of every 141), so
    /// about 98 of a 10-hour day's 360 seconds are spent away. Sparkles flow only while seated, so the seated rate is
    /// divided by this share to keep the same number across a working day.
    /// </summary>
    public const double DeskShare = .73;

    /// <summary>Sparkles per seated worker per real second: more at higher speeds, growing gently and capped at 32x.</summary>
    public static double SparkleRate(double speed) =>
        speed <= 0 ? 0 : SparklesPerSecondAt8x * Math.Sqrt(Math.Min(speed, SpeedCap) / 8) / DeskShare;

    /// <summary>The workers to send a sparkle from this frame, one entry per sparkle.</summary>
    public IReadOnlyList<int> Sparkles(double seconds, double speed, IEnumerable<int> workers)
    {
        var active = workers.ToHashSet();
        foreach (var gone in _due.Keys.Where(id => !active.Contains(id)).ToList()) _due.Remove(gone);
        var rate = SparkleRate(speed);
        var sparkles = new List<int>();
        if (rate <= 0) return sparkles;
        foreach (var id in active.OrderBy(i => i))
        {
            var due = _due.GetValueOrDefault(id) + Math.Max(0, seconds) * rate;
            var count = (int)Math.Floor(due);
            // A long frame (a hitch, or the window coming back) never bursts a backlog of sparkles.
            for (var i = 0; i < Math.Min(count, MaxBurst); i++) sparkles.Add(id);
            _due[id] = due - count;
        }
        return sparkles;
    }

    /// <summary>Compares the shown series' stages with the last look and queues a bubble for each stage that finished.</summary>
    public void Observe(int seriesId, IEnumerable<(int Chapter, Stage Stage, bool Done)> stages)
    {
        var list = stages.ToList();
        var done = list.Where(s => s.Done).Select(s => (s.Chapter, s.Stage)).ToHashSet();
        var chapters = list.Select(s => s.Chapter).ToHashSet();
        // A new series, a load or a chapter that arrives already drawn is the starting point, not finished work.
        if (seriesId != _series || _chapters is null) { _series = seriesId; _chapters = chapters; _done = done; return; }
        foreach (var finished in done.Except(_done).Where(d => _chapters.Contains(d.Chapter)).OrderBy(d => d.Chapter).ThenBy(d => d.Stage))
            if (_bubbles.Count < MaxQueued) _bubbles.Add((finished.Stage, 0));
        _chapters = chapters; _done = done;
    }

    /// <summary>The next stage to announce, at most one per second; bubbles waiting longer than 3 seconds are dropped.</summary>
    public Stage? NextBubble(double seconds)
    {
        _sinceBubble += Math.Max(0, seconds);
        for (var i = 0; i < _bubbles.Count; i++) _bubbles[i] = (_bubbles[i].Stage, _bubbles[i].Age + Math.Max(0, seconds));
        _bubbles.RemoveAll(b => b.Age > BubbleLifetime);
        if (_bubbles.Count == 0 || _sinceBubble < BubbleSpacing) return null;
        _sinceBubble = 0;
        var next = _bubbles[0].Stage; _bubbles.RemoveAt(0);
        return next;
    }

    public void Reset() { _due.Clear(); _bubbles.Clear(); _chapters = null; _done = new(); _series = 0; _sinceBubble = BubbleSpacing; }
}
