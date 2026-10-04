namespace MangakaSim;

public enum MusicPool { Title, Day, Night, Studio, Convention, Deadline, GoodNews, Setback }
/// <summary>Big moments in ascending priority: a setback outranks good news, which outranks a deadline, then a convention.</summary>
public enum MusicMoment { Convention, Deadline, GoodNews, Setback }
public enum MusicTransition { None, FadeIn, Crossfade, FadeOut }
public readonly record struct MusicCommand(MusicTransition Transition, string? TrackId);
public readonly record struct MusicContext(bool InMenu, DateTime GameTime, bool MovedOut, bool Overnight);

/// <summary>
/// Decides what music plays and when (spec 2026-09-28, Q35 gentle, Q36 quiet stretches). Presentation only: it has its
/// own random generator and never touches the simulation, so music cannot change play or save replays.
/// </summary>
public sealed class MusicPlan
{
    public const double FirstTrackDelay = 5;
    /// <summary>Quiet between rotation tracks (Q58, 2026-10-03; was 60 to 120 seconds under Q36).</summary>
    public const double QuietMin = 15, QuietMax = 30;
    private static readonly (string Prefix, MusicPool Pool)[] Prefixes =
    [
        ("title-", MusicPool.Title), ("day-", MusicPool.Day), ("night-", MusicPool.Night), ("studio-", MusicPool.Studio),
        ("convention-", MusicPool.Convention), ("deadline-", MusicPool.Deadline), ("good-news-", MusicPool.GoodNews), ("setback-", MusicPool.Setback),
    ];
    private static readonly string[] Extensions = [".ogg", ".mp3", ".wav"];
    private readonly Dictionary<MusicPool, List<string>> _pools = new();
    private readonly Dictionary<MusicMoment, DateTime> _lastMomentDay = new();
    private readonly Random _random;
    private readonly double _gapMin, _gapMax;
    private MusicMoment? _pending, _playingMoment;
    private bool _inMenu;
    private double _gapLeft = FirstTrackDelay;
    private string? _last;

    public string? Current { get; private set; }

    public MusicPlan(IEnumerable<string> trackIds, int seed, double gapMin = QuietMin, double gapMax = QuietMax)
    {
        _random = new Random(seed); _gapMin = gapMin; _gapMax = gapMax;
        foreach (var id in trackIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(i => i, StringComparer.Ordinal))
            if (PoolOf(id) is { } pool)
            {
                if (!_pools.TryGetValue(pool, out var list)) _pools[pool] = list = new();
                list.Add(id);
            }
    }

    public static MusicPool? PoolOf(string trackId)
    {
        foreach (var (prefix, pool) in Prefixes)
            if (trackId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return pool;
        return null;
    }

    /// <summary>Music files by track name. Exported Godot builds list imported files with ".import" or ".remap" added.</summary>
    public static IReadOnlyDictionary<string, string> Discover(IEnumerable<string> fileNames, string folder) =>
        fileNames.Select(f => f.EndsWith(".import", StringComparison.Ordinal) ? f[..^7] : f.EndsWith(".remap", StringComparison.Ordinal) ? f[..^6] : f)
            .Where(f => Extensions.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)) && PoolOf(Path.GetFileNameWithoutExtension(f)) is not null)
            .GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => folder + "/" + g.OrderBy(x => x, StringComparer.Ordinal).First(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Drops a track for the session, for example when its file fails to load.</summary>
    public void Remove(string trackId)
    {
        foreach (var list in _pools.Values) list.RemoveAll(t => string.Equals(t, trackId, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(Current, trackId, StringComparison.OrdinalIgnoreCase)) { Current = null; _playingMoment = null; _gapLeft = Gap(); }
    }

    public void Notice(IEnumerable<MusicMoment> moments, MusicContext context)
    {
        if (context.InMenu || context.Overnight) return;
        foreach (var moment in moments.Distinct().OrderByDescending(m => m))
        {
            if (!Has(PoolFor(moment))) continue;
            if (_lastMomentDay.TryGetValue(moment, out var day) && day == context.GameTime.Date) continue;
            _lastMomentDay[moment] = context.GameTime.Date;
            if (_pending is null || moment > _pending) _pending = moment;
        }
    }

    public MusicCommand Update(double seconds, MusicContext context, bool trackFinished)
    {
        var playing = Current is not null && !trackFinished;
        if (context.InMenu)
        {
            if (_inMenu && playing) return default;
            _inMenu = true; _pending = null; _playingMoment = null;
            if (Pick(MusicPool.Title) is { } title) { Current = title; return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, title); }
            Current = null;
            return playing ? new(MusicTransition.FadeOut, null) : default;
        }
        if (_inMenu)
        {
            _inMenu = false;
            if (PickRotation(context) is { } next) { Current = next; return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, next); }
            Current = null; _gapLeft = Gap();
            return playing ? new(MusicTransition.FadeOut, null) : default;
        }
        if (_pending is { } moment)
        {
            _pending = null;
            if (!context.Overnight && (_playingMoment is null || moment > _playingMoment) && Pick(PoolFor(moment)) is { } track)
            {
                Current = track; _playingMoment = moment;
                return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, track);
            }
        }
        if (Current is not null)
        {
            if (!trackFinished) return default;
            Current = null; _playingMoment = null; _gapLeft = Gap();
            return default;
        }
        _gapLeft -= seconds;
        if (_gapLeft > 0) return default;
        if (PickRotation(context) is not { } rotation) { _gapLeft = Gap(); return default; }
        Current = rotation;
        return new(MusicTransition.FadeIn, rotation);
    }

    private string? PickRotation(MusicContext context) =>
        context.GameTime.Hour is >= 6 and < 18
            ? context.MovedOut ? Pick(MusicPool.Day, MusicPool.Studio) : Pick(MusicPool.Day)
            : Pick(MusicPool.Night);

    private string? Pick(params MusicPool[] pools)
    {
        var options = pools.SelectMany(p => _pools.TryGetValue(p, out var list) ? list : []).ToList();
        if (options.Count == 0) return null;
        if (options.Count > 1 && _last is not null) options.Remove(_last);
        var pick = options[_random.Next(options.Count)];
        _last = pick;
        return pick;
    }

    private bool Has(MusicPool pool) => _pools.TryGetValue(pool, out var list) && list.Count > 0;
    private double Gap() => _gapMin + _random.NextDouble() * (_gapMax - _gapMin);
    private static MusicPool PoolFor(MusicMoment moment) => moment switch
    {
        MusicMoment.Setback => MusicPool.Setback,
        MusicMoment.GoodNews => MusicPool.GoodNews,
        MusicMoment.Deadline => MusicPool.Deadline,
        _ => MusicPool.Convention,
    };
}
