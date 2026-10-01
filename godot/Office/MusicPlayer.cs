using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// Plays what MusicPlan decides (spec 2026-09-28): two players for crossfades, volume, and a fade on window focus loss.
public partial class MusicPlayer : Node
{
    public const string Folder = "res://Assets/Music";
    private const float CrossfadeSeconds = 3, FadeInSeconds = 2, FadeOutSeconds = 1.5f, FocusSeconds = 1;
    private readonly AudioStreamPlayer[] _players = [new(), new()];
    private readonly float[] _gain = [0, 0], _target = [0, 0];
    private IReadOnlyDictionary<string, Func<AudioStream?>> _tracks = new Dictionary<string, Func<AudioStream?>>();
    private MusicPlan _plan = new([], 0);
    private int _active;
    private float _rate = 1 / FadeInSeconds, _focus = 1;
    private bool _started;

    public Action<string>? Failed { get; set; }
    public string? Current => _plan.Current;
    public float ActiveGain => _gain[_active];
    public float InactiveGain => _gain[1 - _active];
    public float ActiveVolumeDb => _players[_active].VolumeDb;
    public bool Paused => _players[_active].StreamPaused;
    public bool Audible => _players.Any(p => p.Playing && !p.StreamPaused);

    public override void _Ready() { foreach (var player in _players) { player.Bus = "Music"; AddChild(player); } }

    public void UseFolder(int seed)
    {
        var files = DirAccess.DirExistsAbsolute(Folder) ? DirAccess.GetFilesAt(Folder) : [];
        UseTracks(MusicPlan.Discover(files, Folder).ToDictionary(p => p.Key,
            p => (Func<AudioStream?>)(() => ResourceLoader.Exists(p.Value) ? ResourceLoader.Load<AudioStream>(p.Value) : null)), seed);
    }

    public void UseTracks(IReadOnlyDictionary<string, Func<AudioStream?>> tracks, int seed, double gapMin = 60, double gapMax = 120)
    {
        _tracks = tracks; _plan = new MusicPlan(tracks.Keys, seed, gapMin, gapMax);
        foreach (var player in _players) player.Stop();
        _gain[0] = _gain[1] = _target[0] = _target[1] = 0; _started = false;
    }

    public void Notice(IEnumerable<MusicMoment> moments, MusicContext context) => _plan.Notice(moments, context);

    public void Update(double delta, MusicContext context, double volume, bool focused)
    {
        var active = _players[_active];
        // A track has ended only when it stopped by itself: not paused, not faded out by us.
        var finished = _started && !active.Playing && !active.StreamPaused;
        if (finished) _started = false;
        // Unfocused time does not count toward the quiet gap.
        Apply(_plan.Update(focused ? delta : 0, context, finished));

        _focus = Mathf.MoveToward(_focus, focused ? 1 : 0, (float)delta / FocusSeconds);
        for (var i = 0; i < 2; i++)
        {
            _gain[i] = Mathf.MoveToward(_gain[i], _target[i], (float)delta * _rate);
            if (_gain[i] <= 0 && _target[i] <= 0 && _players[i].Playing) { _players[i].Stop(); if (i == _active) _started = false; }
            var pause = _focus <= 0 && !focused;
            if (_players[i].StreamPaused != pause && (_players[i].Playing || _players[i].StreamPaused)) _players[i].StreamPaused = pause;
            _players[i].VolumeDb = Mathf.LinearToDb(Math.Max(1e-5f, _gain[i] * _focus * (float)Math.Clamp(volume, 0, 1)));
        }
    }

    private void Apply(MusicCommand command)
    {
        switch (command.Transition)
        {
            case MusicTransition.FadeIn: Start(command.TrackId!, 1 / FadeInSeconds); break;
            case MusicTransition.Crossfade: Start(command.TrackId!, 1 / CrossfadeSeconds); break;
            case MusicTransition.FadeOut: _target[_active] = 0; _rate = 1 / FadeOutSeconds; _started = false; break;
        }
    }

    private void Start(string trackId, float rate)
    {
        var stream = _tracks.TryGetValue(trackId, out var load) ? load() : null;
        if (stream is null) { _plan.Remove(trackId); Failed?.Invoke(trackId); return; }
        var next = 1 - _active;
        _target[_active] = 0;
        _players[next].Stream = stream; _players[next].StreamPaused = false; _players[next].Play();
        _gain[next] = 0; _target[next] = 1; _active = next; _rate = rate; _started = true;
    }
}
