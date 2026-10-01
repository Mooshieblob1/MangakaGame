using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Music system (spec 2026-09-28): generated tones stand in for Suno tracks. Headless audio may not advance playback,
    // so natural track ends are covered by MusicPlanTests, not here.
    private async void RunMusicSmoke()
    {
        SetProcess(false);
        try
        {
            await SettleUi();
            // Every real track in Assets/Music is found by name and loads (also covers renamed files in exports).
            var real = MusicPlan.Discover(DirAccess.DirExistsAbsolute(MusicPlayer.Folder) ? DirAccess.GetFilesAt(MusicPlayer.Folder) : [], MusicPlayer.Folder);
            var unloadable = real.Where(p => ResourceLoader.Load<AudioStream>(p.Value) is null).Select(p => p.Key).ToList();
            Check(unloadable.Count == 0, $"Every music file in Assets/Music loads ({real.Count} found{(unloadable.Count > 0 ? "; failed: " + string.Join(", ", unloadable) : "")})");
            GD.Print($"MUSIC TRACKS: {string.Join(", ", real.Keys.OrderBy(k => k))}");

            var failures = new List<string>(); _music.Failed = failures.Add;
            var day = new MusicContext(false, new DateTime(1996, 4, 1, 10, 0, 0), false, false);

            _music.UseTracks(new Dictionary<string, Func<AudioStream?>>(), 1);
            for (var i = 0; i < 200; i++) _music.Update(1, day, .5, true);
            Check(_music.Current is null && !_music.Audible && failures.Count == 0, "With no music files the game stays silent");

            static AudioStream Tone(float hz)
            {
                const int rate = 22050, seconds = 30; var data = new byte[rate * seconds * 2];
                for (var i = 0; i < rate * seconds; i++) { var v = (short)(Math.Sin(2 * Math.PI * hz * i / rate) * 8000); data[i * 2] = (byte)v; data[i * 2 + 1] = (byte)(v >> 8); }
                return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Stereo = false, Data = data };
            }
            _music.UseTracks(new Dictionary<string, Func<AudioStream?>>
            {
                ["day-01"] = () => Tone(440), ["day-02"] = () => Tone(494), ["night-01"] = () => Tone(330),
                ["good-news-01"] = () => Tone(660), ["title-01"] = () => Tone(262), ["setback-01"] = () => null,
            }, 2, .2, .3);
            _music.Update(MusicPlan.FirstTrackDelay + 1, day, 0, true);
            Check(_music.Current?.StartsWith("day-") == true && _music.Audible, "A day track starts after the short opening delay");
            Check(_music.ActiveVolumeDb < -60, "Volume 0 keeps it silent");
            for (var i = 0; i < 30; i++) _music.Update(.1, day, .5, true);
            Check(_music.ActiveVolumeDb > -20, "Raising the volume from 0 makes the track audible");

            _music.Notice([MusicMoment.GoodNews], day); _music.Update(.1, day, .5, true);
            Check(_music.Current == "good-news-01", "Good news crossfades in");
            for (var i = 0; i < 40; i++) _music.Update(.1, day, .5, true);
            Check(_music.ActiveGain > .99f && _music.InactiveGain < .01f, "The crossfade completes in about three seconds");

            for (var i = 0; i < 15; i++) _music.Update(.1, day, .5, false);
            Check(_music.Paused, "Losing window focus fades and pauses the music");
            for (var i = 0; i < 15; i++) _music.Update(.1, day, .5, true);
            Check(!_music.Paused && _music.Current == "good-news-01", "Focus brings the same track back");

            var nextDay = day with { GameTime = day.GameTime.AddDays(1) };
            _music.Notice([MusicMoment.Setback], nextDay); _music.Update(.1, nextDay, .5, true);
            Check(failures.Count == 1 && failures[0] == "setback-01" && _music.Current != "setback-01", "A track that fails to load is skipped and reported once");

            _music.Update(.1, day with { InMenu = true }, .5, true);
            Check(_music.Current == "title-01", "The main menu plays the title track");

            // Final review, updated for the title screen: only the title screen plays the title track.
            CloseTitle();_managementReady=true;OpenSaveMenu();await SettleUi();
            Check(!MusicNow().InMenu, "The Save screen keeps the career music");
            _menu.Hide();_inMenu=false;OpenTitle();await SettleUi();
            Check(MusicNow().InMenu, "The title screen plays the title music");
            CloseTitle();

            GD.Print($"MUSIC SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"MUSIC SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
}
